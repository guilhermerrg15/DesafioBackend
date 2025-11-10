namespace Mottu.Core.DTO
{
    // DTO for creating a new rental
    public record LocacaoCriacaoDto(
        int MotoId,
        int EntregadorId,
        int PlanoDias // 7, 15, 30, 45 or 50 days
    );
}
