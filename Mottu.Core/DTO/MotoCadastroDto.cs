namespace Mottu.Core.DTO
{
    // DTO for moto registration request
    public record MotoCadastroDto(
        int Ano,
        string Modelo,
        string Placa
    );
}
