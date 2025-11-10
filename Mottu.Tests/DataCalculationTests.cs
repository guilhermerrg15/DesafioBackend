namespace Mottu.Tests;

public class DataCalculationTests
{
    [Fact]
    public void CalcularDataInicio_DeveSerMeiaNoiteDoDiaSeguinte()
    {
        // Arrange
        var creationDate = new DateTime(2025, 11, 9, 14, 30, 45);
        
        // Act
        var startDate = creationDate.Date.AddDays(1);
        
        // Assert
        Assert.Equal(new DateTime(2025, 11, 10, 0, 0, 0), startDate);
        Assert.Equal(TimeSpan.Zero, startDate.TimeOfDay);
    }

    [Fact]
    public void CalcularDataInicio_IndependenteDoHorario_DeveSerSempreMeiaNoite()
    {
        // Arrange
        var horarios = new[]
        {
            new DateTime(2025, 11, 9, 0, 0, 0),
            new DateTime(2025, 11, 9, 12, 0, 0),
            new DateTime(2025, 11, 9, 23, 59, 59)
        };
        
        // Act & Assert
        foreach (var creationDate in horarios)
        {
            var startDate = creationDate.Date.AddDays(1);
            Assert.Equal(new DateTime(2025, 11, 10, 0, 0, 0), startDate);
        }
    }

    [Theory]
    [InlineData(7, 2025, 11, 10, 2025, 11, 17)]
    [InlineData(15, 2025, 11, 10, 2025, 11, 25)]
    [InlineData(30, 2025, 11, 10, 2025, 12, 10)]
    [InlineData(45, 2025, 11, 10, 2025, 12, 25)]
    [InlineData(50, 2025, 11, 10, 2025, 12, 30)]
    public void CalcularDataTerminoPrevista_PlanosDiferentes_DeveCalcularCorretamente(
        int planoDias, int anoInicio, int mesInicio, int diaInicio, 
        int anoEsperado, int mesEsperado, int diaEsperado)
    {
        // Arrange
        var startDate = new DateTime(anoInicio, mesInicio, diaInicio);
        
        // Act
        var expectedEndDate = startDate.AddDays(planoDias);
        
        // Assert
        Assert.Equal(new DateTime(anoEsperado, mesEsperado, diaEsperado), expectedEndDate);
    }

    [Fact]
    public void CalcularDiasAtraso_DevolucaoAtrasada_DeveCalcularCorretamente()
    {
        // Arrange
        var expectedEndDate = new DateTime(2025, 11, 17);
        var actualEndDate = new DateTime(2025, 11, 20);
        
        // Act
        var delayDays = (actualEndDate - expectedEndDate).Days;
        
        // Assert
        Assert.Equal(3, delayDays);
    }

    [Fact]
    public void CalcularDiasNaoUsados_DevolucaoAntecipada_DeveCalcularCorretamente()
    {
        // Arrange
        var expectedEndDate = new DateTime(2025, 11, 17);
        var actualEndDate = new DateTime(2025, 11, 14);
        
        // Act
        var unusedDays = (expectedEndDate - actualEndDate).Days;
        
        // Assert
        Assert.Equal(3, unusedDays);
    }

    [Fact]
    public void CalcularDias_DevolucaoNaDataPrevista_DeveSerZero()
    {
        // Arrange
        var expectedEndDate = new DateTime(2025, 11, 17);
        var actualEndDate = new DateTime(2025, 11, 17);
        
        // Act
        var delayDays = (actualEndDate - expectedEndDate).Days;
        var unusedDays = (expectedEndDate - actualEndDate).Days;
        
        // Assert
        Assert.Equal(0, delayDays);
        Assert.Equal(0, unusedDays);
    }
}

