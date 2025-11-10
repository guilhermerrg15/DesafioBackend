namespace Mottu.Core.Entities
{
    public class Notificacao
    {
        public int Id { get; set; }
        public int MotoId { get; set; }
        public int AnoMoto { get; set; }
        public string Mensagem { get; set; }
        public DateTime DataNotificacao { get; set; }
    }
}

