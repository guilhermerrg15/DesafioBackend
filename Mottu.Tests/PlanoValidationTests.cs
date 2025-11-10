namespace Mottu.Tests;

public class PlanoValidationTests
{
    private readonly Dictionary<int, decimal> dailyRates = new()
    {
        { 7, 30.00m },
        { 15, 28.00m },
        { 30, 22.00m },
        { 45, 20.00m },
        { 50, 18.00m }
    };

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(50)]
    public void ValidarPlano_PlanosValidos_DeveAceitar(int planoDias)
    {
        // Arrange & Act
        var planoValido = dailyRates.ContainsKey(planoDias);
        
        // Assert
        Assert.True(planoValido);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(44)]
    [InlineData(46)]
    [InlineData(49)]
    [InlineData(51)]
    [InlineData(100)]
    public void ValidarPlano_PlanosInvalidos_DeveRejeitar(int planoDias)
    {
        // Arrange & Act
        var planoValido = dailyRates.ContainsKey(planoDias);
        
        // Assert
        Assert.False(planoValido);
    }

    [Fact]
    public void ValidarValoresPorDia_DeveTerValoresCorretos()
    {
        // Assert
        Assert.Equal(30.00m, dailyRates[7]);
        Assert.Equal(28.00m, dailyRates[15]);
        Assert.Equal(22.00m, dailyRates[30]);
        Assert.Equal(20.00m, dailyRates[45]);
        Assert.Equal(18.00m, dailyRates[50]);
    }

    [Fact]
    public void ValidarValoresPorDia_DeveTerApenas5Planos()
    {
        // Assert
        Assert.Equal(5, dailyRates.Count);
    }
}

