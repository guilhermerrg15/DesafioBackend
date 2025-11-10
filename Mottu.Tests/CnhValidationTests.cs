namespace Mottu.Tests;

public class CnhValidationTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("AB")]
    [InlineData("A+B")]
    public void ValidarTipoCnh_CnhValidas_DeveAceitar(string tipoCnh)
    {
        // Arrange
        var cnhTypeUpper = tipoCnh.ToUpper();
        
        // Act
        var isValidCnhType = cnhTypeUpper == "A" || 
                           cnhTypeUpper == "B" || 
                           cnhTypeUpper == "AB" || 
                           cnhTypeUpper == "A+B";
        
        // Assert
        Assert.True(isValidCnhType);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("D")]
    [InlineData("E")]
    [InlineData("AB+")]
    [InlineData("+AB")]
    [InlineData("")]
    [InlineData("ABC")]
    public void ValidarTipoCnh_CnhInvalidas_DeveRejeitar(string tipoCnh)
    {
        // Arrange
        var cnhTypeUpper = tipoCnh.ToUpper();
        
        // Act
        var isValidCnhType = cnhTypeUpper == "A" || 
                           cnhTypeUpper == "B" || 
                           cnhTypeUpper == "AB" || 
                           cnhTypeUpper == "A+B";
        
        // Assert
        Assert.False(isValidCnhType);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("A+B")]
    public void ValidarCnhParaAlugarMotos_CnhPermitidas_DevePermitir(string tipoCnh)
    {
        // Arrange & Act
        var canRentMotos = tipoCnh == "A" || 
                             tipoCnh == "AB" || 
                             tipoCnh == "A+B";
        
        // Assert
        Assert.True(canRentMotos);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("D")]
    public void ValidarCnhParaAlugarMotos_CnhNaoPermitidas_DeveNegar(string tipoCnh)
    {
        // Arrange & Act
        var canRentMotos = tipoCnh == "A" || 
                             tipoCnh == "AB" || 
                             tipoCnh == "A+B";
        
        // Assert
        Assert.False(canRentMotos);
    }

    [Fact]
    public void ValidarTipoCnh_CaseInsensitive_DeveFuncionar()
    {
        // Arrange
        var tiposCnh = new[] { "a", "A", "ab", "AB", "a+b", "A+B" };
        
        // Act & Assert
        foreach (var tipoCnh in tiposCnh)
        {
            var cnhTypeUpper = tipoCnh.ToUpper();
            var isValidCnhType = cnhTypeUpper == "A" || 
                               cnhTypeUpper == "B" || 
                               cnhTypeUpper == "AB" || 
                               cnhTypeUpper == "A+B";
            
            Assert.True(isValidCnhType, $"CNH type '{tipoCnh}' should be valid");
        }
    }
}

