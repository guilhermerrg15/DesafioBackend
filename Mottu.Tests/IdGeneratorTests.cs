namespace Mottu.Tests;

public class IdGeneratorTests
{
    [Fact]
    public void GerarIdEntregador_DeveGerarIdConsistenteParaMesmoCNPJ()
    {
        // Arrange
        var cnpj = "12.345.678/0001-90";
        
        // Act
        var id1 = GenerateDeliveryPersonId(cnpj);
        var id2 = GenerateDeliveryPersonId(cnpj);
        
        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 > 0);
    }

    [Fact]
    public void GerarIdEntregador_DeveGerarIdDiferenteParaCNPJsDiferentes()
    {
        // Arrange
        var cnpj1 = "12.345.678/0001-90";
        var cnpj2 = "98.765.432/0001-10";
        
        // Act
        var id1 = GenerateDeliveryPersonId(cnpj1);
        var id2 = GenerateDeliveryPersonId(cnpj2);
        
        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GerarIdEntregador_DeveIgnorarFormatacaoCNPJ()
    {
        // Arrange
        var cnpjFormatado = "12.345.678/0001-90";
        var cnpjLimpo = "12345678000190";
        
        // Act
        var id1 = GenerateDeliveryPersonId(cnpjFormatado);
        var id2 = GenerateDeliveryPersonId(cnpjLimpo);
        
        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GerarIdMoto_DeveGerarIdConsistenteParaMesmaPlaca()
    {
        // Arrange
        var placa = "ABC1234";
        
        // Act
        var id1 = GenerateMotoId(placa);
        var id2 = GenerateMotoId(placa);
        
        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 > 0);
    }

    [Fact]
    public void GerarIdMoto_DeveGerarIdDiferenteParaPlacasDiferentes()
    {
        // Arrange
        var placa1 = "ABC1234";
        var placa2 = "XYZ9876";
        
        // Act
        var id1 = GenerateMotoId(placa1);
        var id2 = GenerateMotoId(placa2);
        
        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GerarIdMoto_DeveSerCaseInsensitive()
    {
        // Arrange
        var placaMinuscula = "abc1234";
        var placaMaiuscula = "ABC1234";
        
        // Act
        var id1 = GenerateMotoId(placaMinuscula);
        var id2 = GenerateMotoId(placaMaiuscula);
        
        // Assert
        Assert.Equal(id1, id2);
    }

    // Helper functions (copied from Program.cs)
    private static int GenerateDeliveryPersonId(string cnpj)
    {
        var cleanCnpj = cnpj.Replace(".", "").Replace("/", "").Replace("-", "");
        return Math.Abs(cleanCnpj.GetHashCode());
    }

    private static int GenerateMotoId(string licensePlate)
    {
        return Math.Abs(licensePlate.ToUpper().GetHashCode());
    }
}

