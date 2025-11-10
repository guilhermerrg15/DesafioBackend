namespace Mottu.Core.Services
{
    public interface IMessageService
    {
        Task PublishMotoCadastradaAsync(int motoId, int ano, string modelo, string placa);
    }
}

