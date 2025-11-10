using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Mottu.Core.Services;

namespace Mottu.Infrastructure.Services
{
    public class RabbitMQMessageService : IMessageService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string ExchangeName = "mottu_exchange";
        private const string QueueName = "moto_cadastrada_queue";
        private const string RoutingKey = "moto.cadastrada";

        public RabbitMQMessageService(string connectionString)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

            // Declarar fila
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

            // Bind fila ao exchange
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);
        }

        public async Task PublishMotoCadastradaAsync(int motoId, int ano, string modelo, string placa)
        {
            var message = new
            {
                MotoId = motoId,
                Ano = ano,
                Modelo = modelo,
                Placa = placa,
                DataCadastro = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: properties,
                body: body);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}

