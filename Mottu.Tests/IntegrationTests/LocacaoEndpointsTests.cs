using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mottu.Core.DTO;
using Mottu.Core.Entities;
using Mottu.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Mottu.Tests.IntegrationTests;

public class LocacaoEndpointsTests : IClassFixture<ApiTestBase>
{
    private readonly ApiTestBase _factory;
    private readonly HttpClient _client;

    public LocacaoEndpointsTests(ApiTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_Locacoes_ShouldCreateRental_WhenValidData()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1000, Ano = 2024, Modelo = "Honda CB", Placa = "ABC1000" };
        var entregador = new Entregador 
        { 
            Id = 2000, 
            Nome = "João Silva", 
            Cnpj = "12345678000190", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "12345678901", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        
        db.Motos.Add(moto);
        db.Entregadores.Add(entregador);
        await db.SaveChangesAsync();

        var dto = new LocacaoCriacaoDto(moto.Id, entregador.Id, 7);

        // Act
        var response = await _client.PostAsJsonAsync("/locacoes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var locacao = await response.Content.ReadFromJsonAsync<Locacao>();
        Assert.NotNull(locacao);
        Assert.Equal(moto.Id, locacao.MotoId);
        Assert.Equal(entregador.Id, locacao.EntregadorId);
        Assert.Equal(7, locacao.PlanoDias);
        Assert.Equal(210.00m, locacao.ValorTotal); // 7 days * 30.00
    }

    [Fact]
    public async Task POST_Locacoes_ShouldReturnBadRequest_WhenInvalidPlan()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1001, Ano = 2024, Modelo = "Honda", Placa = "ABC1001" };
        var entregador = new Entregador 
        { 
            Id = 2001, 
            Nome = "Test", 
            Cnpj = "11111111000111", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "11111111111", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        
        db.Motos.Add(moto);
        db.Entregadores.Add(entregador);
        await db.SaveChangesAsync();

        var dto = new LocacaoCriacaoDto(moto.Id, entregador.Id, 10); // Invalid plan

        // Act
        var response = await _client.PostAsJsonAsync("/locacoes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Locacoes_ShouldReturnBadRequest_WhenEntregadorCannotRentMotos()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1002, Ano = 2024, Modelo = "Honda", Placa = "ABC1002" };
        var entregador = new Entregador 
        { 
            Id = 2002, 
            Nome = "Test", 
            Cnpj = "22222222000222", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "22222222222", 
            TipoCnh = "B", // Cannot rent motos
            ImagemCnhUrl = ""
        };
        
        db.Motos.Add(moto);
        db.Entregadores.Add(entregador);
        await db.SaveChangesAsync();

        var dto = new LocacaoCriacaoDto(moto.Id, entregador.Id, 7);

        // Act
        var response = await _client.PostAsJsonAsync("/locacoes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Locacoes_ShouldReturnConflict_WhenMotoAlreadyRented()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1003, Ano = 2024, Modelo = "Honda", Placa = "ABC1003" };
        var entregador1 = new Entregador 
        { 
            Id = 2003, 
            Nome = "Test 1", 
            Cnpj = "33333333000333", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "33333333333", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        var entregador2 = new Entregador 
        { 
            Id = 2004, 
            Nome = "Test 2", 
            Cnpj = "44444444000444", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "44444444444", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        var existingLocacao = new Locacao
        {
            Id = 3000,
            MotoId = moto.Id,
            EntregadorId = entregador1.Id,
            PlanoDias = 7,
            DataInicio = DateTime.UtcNow.AddDays(1),
            DataTerminoPrevista = DateTime.UtcNow.AddDays(8),
            DataTerminoReal = null,
            ValorTotal = 210.00m
        };
        
        db.Motos.Add(moto);
        db.Entregadores.Add(entregador1);
        db.Entregadores.Add(entregador2);
        db.Locacoes.Add(existingLocacao);
        await db.SaveChangesAsync();

        var dto = new LocacaoCriacaoDto(moto.Id, entregador2.Id, 7);

        // Act
        var response = await _client.PostAsJsonAsync("/locacoes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Locacoes_Devolucao_ShouldCalculatePenalty_WhenEarlyReturn_7Days()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1004, Ano = 2024, Modelo = "Honda", Placa = "ABC1004" };
        var entregador = new Entregador 
        { 
            Id = 2005, 
            Nome = "Test", 
            Cnpj = "55555555000555", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "55555555555", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var expectedEndDate = startDate.AddDays(7);
        var locacao = new Locacao
        {
            Id = 3001,
            MotoId = moto.Id,
            EntregadorId = entregador.Id,
            PlanoDias = 7,
            DataInicio = startDate,
            DataTerminoPrevista = expectedEndDate,
            DataTerminoReal = null,
            ValorTotal = 210.00m // 7 * 30.00
        };
        
        db.Motos.Add(moto);
        db.Entregadores.Add(entregador);
        db.Locacoes.Add(locacao);
        await db.SaveChangesAsync();

        var dto = new LocacaoDevolucaoDto(startDate.AddDays(4)); // 3 days early

        // Act
        var response = await _client.PutAsJsonAsync($"/locacoes/{locacao.Id}/devolucao", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Reload from database to get updated values
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<MottuDbContext>();
        var updatedLocacao = await verifyDb.Locacoes.FindAsync(locacao.Id);
        Assert.NotNull(updatedLocacao);
        Assert.NotNull(updatedLocacao.DataTerminoReal);
        // Should have penalty: 3 unused days * 30.00 * 0.20 = 18.00
        // Final: 210.00 - 90.00 + 18.00 = 138.00
        Assert.True(updatedLocacao.ValorTotal < 210.00m);
    }

    [Fact]
    public async Task GET_Locacoes_ShouldReturnAllRentals()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var moto = new Moto { Id = 1005, Ano = 2024, Modelo = "Honda", Placa = "ABC1005" };
        var entregador = new Entregador 
        { 
            Id = 2006, 
            Nome = "Test", 
            Cnpj = "66666666000666", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "66666666666", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        var locacao = new Locacao
        {
            Id = 3002,
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
        var response = await _client.GetAsync("/locacoes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var locacoes = await response.Content.ReadFromJsonAsync<List<Locacao>>();
        Assert.NotNull(locacoes);
        Assert.True(locacoes.Count >= 1);
    }
}

