using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mottu.Core.DTO;
using Mottu.Core.Entities;
using Mottu.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Mottu.Tests.IntegrationTests;

public class MotoEndpointsTests : IClassFixture<ApiTestBase>
{
    private readonly ApiTestBase _factory;
    private readonly HttpClient _client;

    public MotoEndpointsTests(ApiTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_Motos_ShouldCreateMoto_WhenValidData()
    {
        // Arrange
        var dto = new MotoCadastroDto(2024, "Honda CB 600F", "ABC1234");

        // Act
        var response = await _client.PostAsJsonAsync("/motos", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var moto = await response.Content.ReadFromJsonAsync<Moto>();
        Assert.NotNull(moto);
        Assert.Equal("ABC1234", moto.Placa);
        Assert.Equal(2024, moto.Ano);
        Assert.Equal("Honda CB 600F", moto.Modelo);
    }

    [Fact]
    public async Task POST_Motos_ShouldReturnConflict_WhenPlacaAlreadyExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var existingMoto = new Moto { Id = 123456, Ano = 2023, Modelo = "Yamaha", Placa = "XYZ9876" };
        db.Motos.Add(existingMoto);
        await db.SaveChangesAsync();

        var dto = new MotoCadastroDto(2024, "Honda CB 600F", "XYZ9876");

        // Act
        var response = await _client.PostAsJsonAsync("/motos", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GET_Motos_ShouldReturnAllMotos()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        db.Motos.Add(new Moto { Id = 111, Ano = 2024, Modelo = "Honda", Placa = "ABC1111" });
        db.Motos.Add(new Moto { Id = 222, Ano = 2023, Modelo = "Yamaha", Placa = "XYZ2222" });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/motos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var motos = await response.Content.ReadFromJsonAsync<List<Moto>>();
        Assert.NotNull(motos);
        Assert.True(motos.Count >= 2);
    }

    [Fact]
    public async Task GET_Motos_WithPlacaFilter_ShouldReturnFilteredMotos()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        db.Motos.Add(new Moto { Id = 333, Ano = 2024, Modelo = "Honda", Placa = "ABC3333" });
        db.Motos.Add(new Moto { Id = 444, Ano = 2023, Modelo = "Yamaha", Placa = "XYZ4444" });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/motos?placa=ABC");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var motos = await response.Content.ReadFromJsonAsync<List<Moto>>();
        Assert.NotNull(motos);
        Assert.All(motos, m => Assert.Contains("ABC", m.Placa));
    }

    [Fact]
    public async Task GET_Motos_ById_ShouldReturnMoto_WhenExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 555, Ano = 2024, Modelo = "Honda CB", Placa = "ABC5555" };
        db.Motos.Add(moto);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/motos/{moto.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedMoto = await response.Content.ReadFromJsonAsync<Moto>();
        Assert.NotNull(returnedMoto);
        Assert.Equal(moto.Id, returnedMoto.Id);
        Assert.Equal(moto.Placa, returnedMoto.Placa);
    }

    [Fact]
    public async Task GET_Motos_ById_ShouldReturnNotFound_WhenNotExists()
    {
        // Act
        var response = await _client.GetAsync("/motos/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Motos_Placa_ShouldUpdatePlaca_WhenValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 666, Ano = 2024, Modelo = "Honda", Placa = "ABC6666" };
        db.Motos.Add(moto);
        await db.SaveChangesAsync();

        var dto = new MotoAtualizaPlacaDto("NEW1234");

        // Act
        var response = await _client.PutAsJsonAsync($"/motos/{moto.Id}/placa", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedMoto = await response.Content.ReadFromJsonAsync<Moto>();
        Assert.NotNull(updatedMoto);
        Assert.Equal("NEW1234", updatedMoto.Placa);
    }

    [Fact]
    public async Task DELETE_Motos_ShouldRemoveMoto_WhenNoActiveRentals()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        // Use a unique ID to avoid hash collisions
        var uniqueId = Math.Abs("DELETE_TEST_PLACA".GetHashCode());
        var moto = new Moto { Id = uniqueId, Ano = 2024, Modelo = "Honda", Placa = "DELETE_TEST_PLACA" };
        db.Motos.Add(moto);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/motos/{moto.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify in a new scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MottuDbContext>();
        var motoExists = await verifyDb.Motos.FindAsync(moto.Id);
        Assert.Null(motoExists);
    }

    [Fact]
    public async Task DELETE_Motos_ShouldReturnConflict_WhenHasActiveRentals()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 888, Ano = 2024, Modelo = "Honda", Placa = "ABC8888" };
        var entregador = new Entregador 
        { 
            Id = 100, 
            Nome = "Test", 
            Cnpj = "12345678000190", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "12345678901", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        var locacao = new Locacao
        {
            Id = 200,
            MotoId = moto.Id,
            EntregadorId = entregador.Id,
            PlanoDias = 7,
            DataInicio = DateTime.UtcNow.AddDays(1),
            DataTerminoPrevista = DateTime.UtcNow.AddDays(8),
            DataTerminoReal = null,
            ValorTotal = 210.00m
        };

        db.Motos.Add(moto);
        db.Entregadores.Add(entregador);
        db.Locacoes.Add(locacao);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/motos/{moto.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}

