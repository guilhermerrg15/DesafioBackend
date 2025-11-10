
namespace Mottu.Core.Entities
{
    public class Entregador
    {
    public int Id {get; set; } // identificador (baseado no CNPJ)
    public string Nome {get; set;}
    public string Cnpj {get; set; } //deve ser unico
    public DateTime DataNascimento {get; set; } // deve ser unico
    public string NumeroCnh {get; set; } // deve ser unico

    public string TipoCnh {get; set; } //tipos válidos -> A, B, AB (A+B normalizado)
    public string ImagemCnhUrl {get; set; }

    // propriedade de navegacao
    //public ICollection<Locacao> Locacoes {get; set; } = new List<Locacao>();
    }
}