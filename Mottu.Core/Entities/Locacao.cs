
namespace Mottu.Core.Entities 
{
    public class Locacao
    {
    public int Id {get; set; }
    
    //chaves estrangeiras
    public int MotoId {get; set; }
    public int EntregadorId {get; set;}

    //propriedades de navegacao (EF Core)
    public Moto Moto {get; set; }
    public Entregador Entregador {get; set; }

    public DateTime DataInicio {get; set; } // obrigatório (primeiro dia após a criacao)
    public DateTime DataTerminoPrevista {get; set; } //obrigatório (plano + data início)

    //data real da devolucao (pode ser NULL inicialmente)
    public DateTime? DataTerminoReal {get; set;}
    
    //Plano contratado (7, 15, 30, 45, 50)
    public int PlanoDias {get; set; }
    public decimal ValorTotal {get; set; }
    }
}