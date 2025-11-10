using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mottu.Core.DTO;
using Mottu.Core.Entities;
using Mottu.Core.Services;
using Mottu.Infrastructure.Data;
using Mottu.Infrastructure.Services;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Mottu API",
        Version = "v1",
        Description = "API for moto and delivery person management"
    });
});
builder.Services.AddAntiforgery();

// --- Entity Framework Core Configuration ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection String 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<MottuDbContext>(options =>
{
    options.UseNpgsql(connectionString,
        npgsqlOptions => 
        {
            npgsqlOptions.MigrationsAssembly(typeof(MottuDbContext).Assembly.FullName);
        }
    );
});

// --- RabbitMQ Configuration ---
var rabbitMQConnection = builder.Configuration["RABBITMQ_CONNECTION"]
    ?? builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://mottu_admin:mottu_secure_pass_2024!@localhost:5672/";

builder.Services.AddSingleton<IMessageService>(sp =>
    new RabbitMQMessageService(rabbitMQConnection));

var app = builder.Build();

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mottu API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Helper functions for ID generation
static int GenerateDeliveryPersonId(string cnpj)
{
    var cleanCnpj = cnpj.Replace(".", "").Replace("/", "").Replace("-", "");
    return Math.Abs(cleanCnpj.GetHashCode());
}

static int GenerateMotoId(string licensePlate)
{
    return Math.Abs(licensePlate.ToUpper().GetHashCode());
}

// --- MOTO ENDPOINTS ---

// POST /motos - Register moto
app.MapPost("/motos", async (MottuDbContext db, MotoCadastroDto dto, IMessageService messageService) =>
{
    // Business Rule: License plate must be unique
    var licensePlateExists = await db.Motos.AnyAsync(m => m.Placa == dto.Placa);

    if (licensePlateExists)
    {
        return Results.Conflict(new { message = $"License plate '{dto.Placa}' is already registered." });
    }

    // Generate ID based on license plate
    var motoId = GenerateMotoId(dto.Placa);

    // Check for hash collision
    var idExists = await db.Motos.AnyAsync(m => m.Id == motoId);
    if (idExists)
    {
        var counter = 1;
        while (await db.Motos.AnyAsync(m => m.Id == motoId + counter))
        {
            counter++;
        }
        motoId += counter;
    }

    var newMoto = new Moto
    {
        Id = motoId,
        Ano = dto.Ano,
        Modelo = dto.Modelo,
        Placa = dto.Placa
    };

    db.Motos.Add(newMoto);
    await db.SaveChangesAsync();

    // Publish "moto registered" event via messaging
    try
    {
        await messageService.PublishMotoCadastradaAsync(newMoto.Id, newMoto.Ano, newMoto.Modelo, newMoto.Placa);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error publishing moto registered event: {ex.Message}");
    }

    return Results.Created($"/motos/{newMoto.Id}", newMoto);
})
.WithName("RegisterMoto");

// GET /motos - List motos (optional filter by license plate)
app.MapGet("/motos", async (MottuDbContext db, string? placa) =>
{
    if (!string.IsNullOrWhiteSpace(placa))
    {
        var filteredMotos = await db.Motos
            .Where(m => m.Placa.Contains(placa))
            .ToListAsync();

        return Results.Ok(filteredMotos);
    }

    var motos = await db.Motos.ToListAsync();
    return Results.Ok(motos);
})
.WithName("ListMotos");

// GET /motos/{id} - Get moto by ID
app.MapGet("/motos/{id:int}", async (MottuDbContext db, int id) =>
{
    var moto = await db.Motos.FindAsync(id);

    if (moto is not null)
    {
        return Results.Ok(moto);
    }
    
    return Results.NotFound(new { message = $"Moto with ID '{id}' not found." });
})
.WithName("GetMotoById");

// PUT /motos/{id}/placa - Update license plate
app.MapPut("/motos/{id:int}/placa", async (MottuDbContext db, int id, MotoAtualizaPlacaDto dto) =>
{
    var moto = await db.Motos.FindAsync(id);

    if (moto is null)
    {
        return Results.NotFound(new { message = $"Moto with ID '{id}' not found." });
    }

    // Business Rule: License plate must be unique (except for the same moto)
    var licensePlateExists = await db.Motos
        .AnyAsync(m => m.Placa == dto.Placa && m.Id != id);

    if (licensePlateExists)
    {
        return Results.Conflict(new { message = $"License plate '{dto.Placa}' is already being used by another moto." });
    }

    moto.Placa = dto.Placa;
    await db.SaveChangesAsync();

    return Results.Ok(moto);
})
.WithName("UpdateMotoLicensePlate");

// DELETE /motos/{id} - Remove moto
app.MapDelete("/motos/{id:int}", async (MottuDbContext db, int id) =>
{
    var moto = await db.Motos.FindAsync(id);

    if (moto is null)
    {
        return Results.NotFound(new { message = $"Moto with ID '{id}' not found." });
    }

    // Check if there are active rentals for this moto
    var hasActiveRentals = await db.Locacoes
        .AnyAsync(l => l.MotoId == id && l.DataTerminoReal == null);

    if (hasActiveRentals)
    {
        return Results.Conflict(new { message = "Cannot remove moto. There are active rentals associated with it." });
    }

    db.Motos.Remove(moto);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("RemoveMoto");

// --- DELIVERY PERSON ENDPOINTS ---

var uploadDirectory = Path.Combine(AppContext.BaseDirectory, "storage");

if (!Directory.Exists(uploadDirectory))
{
    Directory.CreateDirectory(uploadDirectory);
}

// POST /entregadores - Register delivery person
app.MapPost("/entregadores", async (MottuDbContext db, EntregadorCadastroDto dto) =>
{
    // Business Rule: CNPJ must be unique
    var cnpjExists = await db.Entregadores.AnyAsync(e => e.Cnpj == dto.Cnpj);
    if (cnpjExists)
    {
        return Results.Conflict(new { message = $"CNPJ '{dto.Cnpj}' is already registered." });
    }

    // Business Rule: CNH number must be unique
    var cnhExists = await db.Entregadores.AnyAsync(e => e.NumeroCnh == dto.NumeroCnh);
    if (cnhExists)
    {
        return Results.Conflict(new { message = $"CNH number '{dto.NumeroCnh}' is already registered." });
    }

    // Business Rule: Valid CNH type (A, B, AB or A+B)
    var cnhTypeUpper = dto.TipoCnh.ToUpper();
    var isValidCnhType = cnhTypeUpper == "A" || cnhTypeUpper == "B" || cnhTypeUpper == "AB" || cnhTypeUpper == "A+B";
    if (!isValidCnhType)
    {
        return Results.BadRequest(new { message = "CNH type must be 'A', 'B', 'AB' or 'A+B'." });
    }

    // Generate ID based on CNPJ
    var deliveryPersonId = GenerateDeliveryPersonId(dto.Cnpj);

    // Check for hash collision
    var idExists = await db.Entregadores.AnyAsync(e => e.Id == deliveryPersonId);
    if (idExists)
    {
        var counter = 1;
        while (await db.Entregadores.AnyAsync(e => e.Id == deliveryPersonId + counter))
        {
            counter++;
        }
        deliveryPersonId += counter;
    }

    var newDeliveryPerson = new Entregador
    {
        Id = deliveryPersonId,
        Nome = dto.Nome,
        Cnpj = dto.Cnpj,
        DataNascimento = dto.DataNascimento.ToUniversalTime(),
        NumeroCnh = dto.NumeroCnh,
        TipoCnh = cnhTypeUpper, // Keeps "A+B" if provided, doesn't normalize to "AB"
        ImagemCnhUrl = string.Empty // Will be updated by the next endpoint
    };

    db.Entregadores.Add(newDeliveryPerson);
    await db.SaveChangesAsync();

    return Results.Created($"/entregadores/{newDeliveryPerson.Id}", newDeliveryPerson);
})
.WithName("RegisterDeliveryPerson");

// POST /entregadores/{id}/cnh - Upload CNH photo
app.MapPost("/entregadores/{id:int}/cnh", async (
    MottuDbContext db, 
    int id,
    IFormFile file) =>
{
    var deliveryPerson = await db.Entregadores.FindAsync(id);
    
    if (deliveryPerson is null)
    {
        return Results.NotFound(new { message = $"Delivery person with ID '{id}' not found." });
    }

    // Business Rule: CNH type A, AB or A+B (Moto)
    var canRentMotos = deliveryPerson.TipoCnh == "A" || deliveryPerson.TipoCnh == "AB" || deliveryPerson.TipoCnh == "A+B";
    if (!canRentMotos)
    {
        return Results.BadRequest(new { message = $"CNH type '{deliveryPerson.TipoCnh}' is not valid for moto rental. Must be 'A', 'AB' or 'A+B'." });
    }

    // Validate and save file
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "No CNH file uploaded." });
    }

    var allowedExtensions = new[] { ".png", ".bmp" };
    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(fileExtension))
    {
        return Results.BadRequest(new { message = $"File format not supported. Use {string.Join(" or ", allowedExtensions)}." });
    }
    
    var uniqueFileName = $"{id}_cnh{fileExtension}";
    var filePath = Path.Combine(uploadDirectory, uniqueFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    deliveryPerson.ImagemCnhUrl = uniqueFileName;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "CNH saved successfully.",
        entregadorId = id,
        cnhUrl = uniqueFileName 
    });
})
.WithName("UploadCnh")
.DisableAntiforgery();

// --- RENTAL ENDPOINTS ---

// Daily rates for each rental plan
var dailyRates = new Dictionary<int, decimal>
{
    { 7, 30.00m },
    { 15, 28.00m },
    { 30, 22.00m },
    { 45, 20.00m },
    { 50, 18.00m }
};

// POST /locacoes - Create rental
app.MapPost("/locacoes", async (MottuDbContext db, LocacaoCriacaoDto dto) =>
{
    // Validate plan
    if (!dailyRates.ContainsKey(dto.PlanoDias))
    {
        return Results.BadRequest(new { message = $"Invalid plan. Available plans: 7, 15, 30, 45 or 50 days." });
    }

    // Validate moto exists
    var moto = await db.Motos.FindAsync(dto.MotoId);
    if (moto is null)
    {
        return Results.NotFound(new { message = $"Moto with ID '{dto.MotoId}' not found." });
    }

    // Validate delivery person exists and has CNH type A, AB or A+B
    var deliveryPerson = await db.Entregadores.FindAsync(dto.EntregadorId);
    if (deliveryPerson is null)
    {
        return Results.NotFound(new { message = $"Delivery person with ID '{dto.EntregadorId}' not found." });
    }

    var canRentMotos = deliveryPerson.TipoCnh == "A" || deliveryPerson.TipoCnh == "AB" || deliveryPerson.TipoCnh == "A+B";
    if (!canRentMotos)
    {
        return Results.BadRequest(new { message = $"Delivery person must have CNH type 'A', 'AB' or 'A+B' to rent motos." });
    }

    // Check if moto is already rented (has active rental)
    var motoAlreadyRented = await db.Locacoes
        .AnyAsync(l => l.MotoId == dto.MotoId && l.DataTerminoReal == null);

    if (motoAlreadyRented)
    {
        return Results.Conflict(new { message = "Moto is already rented." });
    }

    // Calculate dates and values
    var creationDate = DateTime.UtcNow;
    // Start date = first day after creation (midnight of next day)
    var startDate = creationDate.Date.AddDays(1);
    // Expected end date = start date + plan days
    var expectedEndDate = startDate.AddDays(dto.PlanoDias);
    var dailyRate = dailyRates[dto.PlanoDias];
    var totalValue = dailyRate * dto.PlanoDias;

    // Generate rental ID (based on timestamp + motoId + deliveryPersonId)
    var rentalId = Math.Abs((creationDate.Ticks.ToString() + dto.MotoId.ToString() + dto.EntregadorId.ToString()).GetHashCode());

    // Check for hash collision
    var idExists = await db.Locacoes.AnyAsync(l => l.Id == rentalId);
    if (idExists)
    {
        var counter = 1;
        while (await db.Locacoes.AnyAsync(l => l.Id == rentalId + counter))
        {
            counter++;
        }
        rentalId += counter;
    }

    var newRental = new Locacao
    {
        Id = rentalId,
        MotoId = dto.MotoId,
        EntregadorId = dto.EntregadorId,
        PlanoDias = dto.PlanoDias,
        DataInicio = startDate,
        DataTerminoPrevista = expectedEndDate,
        DataTerminoReal = null,
        ValorTotal = totalValue
    };

    db.Locacoes.Add(newRental);
    await db.SaveChangesAsync();

    // Load relationships to return complete data
    await db.Entry(newRental).Reference(l => l.Moto).LoadAsync();
    await db.Entry(newRental).Reference(l => l.Entregador).LoadAsync();

    return Results.Created($"/locacoes/{newRental.Id}", newRental);
})
.WithName("CreateRental");

// GET /locacoes - List rentals
app.MapGet("/locacoes", async (MottuDbContext db) =>
{
    var rentals = await db.Locacoes
        .Include(l => l.Moto)
        .Include(l => l.Entregador)
        .ToListAsync();

    return Results.Ok(rentals);
})
.WithName("ListRentals");

// GET /locacoes/{id} - Get rental by ID
app.MapGet("/locacoes/{id:int}", async (MottuDbContext db, int id) =>
{
    var rental = await db.Locacoes
        .Include(l => l.Moto)
        .Include(l => l.Entregador)
        .FirstOrDefaultAsync(l => l.Id == id);

    if (rental is not null)
    {
        return Results.Ok(rental);
    }

    return Results.NotFound(new { message = $"Rental with ID '{id}' not found." });
})
.WithName("GetRentalById");

// PUT /locacoes/{id}/devolucao - Return rental
app.MapPut("/locacoes/{id:int}/devolucao", async (MottuDbContext db, int id, LocacaoDevolucaoDto dto) =>
{
    var rental = await db.Locacoes
        .Include(l => l.Moto)
        .Include(l => l.Entregador)
        .FirstOrDefaultAsync(l => l.Id == id);

    if (rental is null)
    {
        return Results.NotFound(new { message = $"Rental with ID '{id}' not found." });
    }

    // Check if already returned
    if (rental.DataTerminoReal.HasValue)
    {
        return Results.BadRequest(new { message = $"Rental was already returned on {rental.DataTerminoReal.Value:yyyy-MM-dd}." });
    }

    // Calculate total value considering penalties or additional daily rates
    var actualEndDate = dto.DataTerminoReal.Date;
    var expectedEndDate = rental.DataTerminoPrevista.Date;
    var dailyRate = dailyRates[rental.PlanoDias];
    decimal totalValue = rental.ValorTotal; // Base value already calculated

    // If returned before expected date
    if (actualEndDate < expectedEndDate)
    {
        var unusedDays = (expectedEndDate - actualEndDate).Days;
        var unusedDaysValue = dailyRate * unusedDays;

        // Calculate penalty based on plan
        decimal penaltyPercentage = 0;
        if (rental.PlanoDias == 7)
        {
            penaltyPercentage = 0.20m; // 20%
        }
        else if (rental.PlanoDias == 15)
        {
            penaltyPercentage = 0.40m; // 40%
        }
        // For 30, 45 and 50 day plans, no penalty specified in the challenge

        if (penaltyPercentage > 0)
        {
            var penalty = unusedDaysValue * penaltyPercentage;
            totalValue = rental.ValorTotal - unusedDaysValue + penalty;
        }
        else
        {
            // For other plans, just discount unused days
            totalValue = rental.ValorTotal - unusedDaysValue;
        }
    }
    // If returned after expected date
    else if (actualEndDate > expectedEndDate)
    {
        var additionalDays = (actualEndDate - expectedEndDate).Days;
        var additionalValue = 50.00m * additionalDays; // R$ 50.00 per additional day
        totalValue = rental.ValorTotal + additionalValue;
    }
    // If returned exactly on expected date, keep original value

    // Update rental
    rental.DataTerminoReal = actualEndDate;
    rental.ValorTotal = totalValue;

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        rental,
        valorTotalCalculado = totalValue,
        mensagem = "Rental returned successfully."
    });
})
.WithName("ReturnRental");

// --- RABBITMQ CONSUMER FOR 2024 MOTOS ---
var messageServiceInstance = app.Services.GetRequiredService<IMessageService>();
var dbContextFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

Task.Run(async () =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(rabbitMQConnection)
    };

    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    channel.QueueDeclare("moto_cadastrada_queue", durable: true, exclusive: false, autoDelete: false);

    var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
    consumer.Received += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            var motoEvent = JsonSerializer.Deserialize<MotoRegisteredEvent>(message);

            if (motoEvent != null && motoEvent.Ano == 2024)
            {
                // Create notification for 2024 motos
                using var scope = dbContextFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();

                // Generate notification ID based on timestamp + motoId
                var notificationId = Math.Abs((DateTime.UtcNow.Ticks.ToString() + motoEvent.MotoId.ToString()).GetHashCode());

                var notification = new Notificacao
                {
                    Id = notificationId,
                    MotoId = motoEvent.MotoId,
                    AnoMoto = motoEvent.Ano,
                    Mensagem = $"Moto {motoEvent.Modelo} (License Plate: {motoEvent.Placa}) from 2024 was registered.",
                    DataNotificacao = DateTime.UtcNow
                };

                db.Notificacoes.Add(notification);
                await db.SaveChangesAsync();

                Console.WriteLine($"Notification created for 2024 moto: {motoEvent.Placa}");
            }

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
        }
    };

    channel.BasicConsume("moto_cadastrada_queue", autoAck: false, consumer);

    Console.WriteLine("RabbitMQ consumer started. Waiting for messages...");

    // Keep consumer running
    await Task.Delay(Timeout.Infinite);
});

app.Run();

// Helper classes
record MotoRegisteredEvent(int MotoId, int Ano, string Modelo, string Placa, DateTime DataCadastro);
