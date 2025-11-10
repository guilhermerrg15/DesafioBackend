
namespace Mottu.Core.Entities 
{
    public class Moto 
{
    public int Id {get; set; } //identificador único (baseado na placa)
    public int Ano {get; set; }
    public string Modelo {get; set; }
    public string Placa {get; set; } // unico

    //propridade de navegacao para locacao, se houver
    //public ICollection<Locacao> Locacoes {get; set; } = new List<Locacao>();
}
}