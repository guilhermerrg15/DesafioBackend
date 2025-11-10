namespace Mottu.Tests;

public class LocacaoCalculoTests
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
    [InlineData(7, 30.00, 210.00)]
    [InlineData(15, 28.00, 420.00)]
    [InlineData(30, 22.00, 660.00)]
    [InlineData(45, 20.00, 900.00)]
    [InlineData(50, 18.00, 900.00)]
    public void CalcularValorTotalLocacao_DeveCalcularCorretamente(int planoDias, decimal valorPorDiaEsperado, decimal valorTotalEsperado)
    {
        // Arrange
        var dailyRate = dailyRates[planoDias];
        
        // Act
        var totalValue = dailyRate * planoDias;
        
        // Assert
        Assert.Equal(valorPorDiaEsperado, dailyRate);
        Assert.Equal(valorTotalEsperado, totalValue);
    }

    [Fact]
    public void CalcularDataInicio_DeveSerPrimeiroDiaAposCriacao()
    {
        // Arrange
        var creationDate = new DateTime(2025, 11, 9, 14, 30, 0);
        
        // Act
        var startDate = creationDate.Date.AddDays(1);
        
        // Assert
        Assert.Equal(new DateTime(2025, 11, 10, 0, 0, 0), startDate);
    }

    [Fact]
    public void CalcularDataTerminoPrevista_DeveSerDataInicioMaisPlanoDias()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 10);
        var planDays = 7;
        
        // Act
        var expectedEndDate = startDate.AddDays(planDays);
        
        // Assert
        Assert.Equal(new DateTime(2025, 11, 17), expectedEndDate);
    }

    [Fact]
    public void CalcularMultaDevolucaoAntecipada_Plano7Dias_DeveAplicar20Porcento()
    {
        // Arrange
        var planDays = 7;
        var dailyRate = dailyRates[planDays];
        var totalValue = dailyRate * planDays; // 210.00
        var unusedDays = 3;
        var unusedDaysValue = dailyRate * unusedDays; // 90.00
        var penaltyPercentage = 0.20m;
        
        // Act
        var penalty = unusedDaysValue * penaltyPercentage; // 18.00
        var finalTotalValue = totalValue - unusedDaysValue + penalty; // 210 - 90 + 18 = 138
        
        // Assert
        Assert.Equal(18.00m, penalty);
        Assert.Equal(138.00m, finalTotalValue);
    }

    [Fact]
    public void CalcularMultaDevolucaoAntecipada_Plano15Dias_DeveAplicar40Porcento()
    {
        // Arrange
        var planDays = 15;
        var dailyRate = dailyRates[planDays];
        var totalValue = dailyRate * planDays; // 420.00
        var unusedDays = 5;
        var unusedDaysValue = dailyRate * unusedDays; // 140.00
        var penaltyPercentage = 0.40m;
        
        // Act
        var penalty = unusedDaysValue * penaltyPercentage; // 56.00
        var finalTotalValue = totalValue - unusedDaysValue + penalty; // 420 - 140 + 56 = 336
        
        // Assert
        Assert.Equal(56.00m, penalty);
        Assert.Equal(336.00m, finalTotalValue);
    }

    [Fact]
    public void CalcularMultaDevolucaoAntecipada_Plano30Dias_NaoDeveAplicarMulta()
    {
        // Arrange
        var planDays = 30;
        var dailyRate = dailyRates[planDays];
        var totalValue = dailyRate * planDays; // 660.00
        var unusedDays = 5;
        var unusedDaysValue = dailyRate * unusedDays; // 110.00
        
        // Act
        var finalTotalValue = totalValue - unusedDaysValue; // 660 - 110 = 550
        
        // Assert
        Assert.Equal(550.00m, finalTotalValue);
    }

    [Fact]
    public void CalcularDiariasAdicionais_DevolucaoAtrasada_DeveCobrar50ReaisPorDia()
    {
        // Arrange
        var baseTotalValue = 210.00m; // 7 day plan
        var delayDays = 3;
        var additionalDailyRate = 50.00m;
        
        // Act
        var additionalValue = delayDays * additionalDailyRate; // 150.00
        var finalTotalValue = baseTotalValue + additionalValue; // 210 + 150 = 360
        
        // Assert
        Assert.Equal(150.00m, additionalValue);
        Assert.Equal(360.00m, finalTotalValue);
    }

    [Fact]
    public void CalcularDevolucaoNaDataPrevista_DeveManterValorOriginal()
    {
        // Arrange
        var totalValue = 210.00m;
        
        // Act
        var finalTotalValue = totalValue; // No changes
        
        // Assert
        Assert.Equal(210.00m, finalTotalValue);
    }

    [Theory]
    [InlineData(7, 3, 138.00)] // 210 - 90 + 18 (multa 20%)
    [InlineData(15, 5, 336.00)] // 420 - 140 + 56 (multa 40%)
    [InlineData(30, 5, 550.00)] // 660 - 110 (sem multa)
    public void CalcularDevolucaoAntecipada_CenariosVariados_DeveCalcularCorretamente(
        int planoDias, int diasNaoUsados, decimal valorEsperado)
    {
        // Arrange
        var dailyRate = dailyRates[planoDias];
        var totalValue = dailyRate * planoDias;
        var unusedDaysValue = dailyRate * diasNaoUsados;
        
        decimal penaltyPercentage = 0;
        if (planoDias == 7)
            penaltyPercentage = 0.20m;
        else if (planoDias == 15)
            penaltyPercentage = 0.40m;
        
        // Act
        decimal finalTotalValue;
        if (penaltyPercentage > 0)
        {
            var penalty = unusedDaysValue * penaltyPercentage;
            finalTotalValue = totalValue - unusedDaysValue + penalty;
        }
        else
        {
            finalTotalValue = totalValue - unusedDaysValue;
        }
        
        // Assert
        Assert.Equal(valorEsperado, finalTotalValue);
    }
}

