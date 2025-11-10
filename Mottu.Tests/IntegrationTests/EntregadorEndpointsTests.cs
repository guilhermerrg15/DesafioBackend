using System.Net;
using System.Net.Http.Json;
using Mottu.Core.DTO;
using Mottu.Core.Entities;
using Mottu.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Mottu.Tests.IntegrationTests;

public class EntregadorEndpointsTests : IClassFixture<ApiTestBase>
{
    private readonly ApiTestBase _factory;
    private readonly HttpClient _client;

    public EntregadorEndpointsTests(ApiTestBase factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_Entregadores_ShouldCreateEntregador_WhenValidData()
    {
        // Arrange - Use unique CNPJ and CNH to avoid conflicts
        var uniqueCnpj = $"99999999000{DateTime.UtcNow.Ticks % 10000}";
        var uniqueCnh = $"999999999{DateTime.UtcNow.Ticks % 10000}";
        var dto = new EntregadorCadastroDto(
            "João Silva",
            uniqueCnpj,
            new DateTime(1990, 5, 15),
            uniqueCnh,
            "A"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/entregadores", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var entregador = await response.Content.ReadFromJsonAsync<Entregador>();
        Assert.NotNull(entregador);
        Assert.Equal("João Silva", entregador.Nome);
        Assert.Equal(uniqueCnpj, entregador.Cnpj);
        Assert.Equal("A", entregador.TipoCnh);
    }

    [Fact]
    public async Task POST_Entregadores_ShouldReturnConflict_WhenCnpjAlreadyExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
        
        var existingEntregador = new Entregador 
        { 
            Id = 100, 
            Nome = "Existing", 
            Cnpj = "12345678000190", 
            DataNascimento = DateTime.UtcNow, 
            NumeroCnh = "99999999999", 
            TipoCnh = "A",
            ImagemCnhUrl = ""
        };
        db.Entregadores.Add(existingEntregador);
        await db.SaveChangesAsync();

        var dto = new EntregadorCadastroDto(
            "João Silva",
            "12345678000190",
            new DateTime(1990, 5, 15),
            "12345678901",
            "A"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/entregadores", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task POST_Entregadores_ShouldReturnBadRequest_WhenInvalidCnhType()
    {
        // Arrange - Use unique CNPJ and CNH to avoid conflicts
        var uniqueCnpj = $"88888888000{DateTime.UtcNow.Ticks % 10000}";
        var uniqueCnh = $"888888888{DateTime.UtcNow.Ticks % 10000}";
        var dto = new EntregadorCadastroDto(
            "João Silva",
            uniqueCnpj,
            new DateTime(1990, 5, 15),
            uniqueCnh,
            "C" // Invalid CNH type
        );

        // Act
        var response = await _client.PostAsJsonAsync("/entregadores", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Entregadores_ShouldAcceptAplusB_CnhType()
    {
        // Arrange
        var dto = new EntregadorCadastroDto(
            "João Silva",
            "98765432000110",
            new DateTime(1990, 5, 15),
            "98765432109",
            "A+B"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/entregadores", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var entregador = await response.Content.ReadFromJsonAsync<Entregador>();
        Assert.NotNull(entregador);
        Assert.Equal("A+B", entregador.TipoCnh);
    }
}

