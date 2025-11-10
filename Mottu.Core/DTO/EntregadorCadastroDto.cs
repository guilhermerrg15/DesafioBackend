namespace Mottu.Core.DTO
{
    // DTO for delivery person registration request
    public record EntregadorCadastroDto(
        string Nome,
        string Cnpj,
        DateTime DataNascimento,
        string NumeroCnh,
        string TipoCnh
    );
}
