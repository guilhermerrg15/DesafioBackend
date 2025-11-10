using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Mottu.Core.Services;

namespace Mottu.Infrastructure.Services
{
    public class RabbitMQMessageService : IMessageService, IDisposable
    {
        private readonly string _connectionString;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly object _lock = new object();
        private const string ExchangeName = "mottu_exchange";
        private const string QueueName = "moto_cadastrada_queue";
        private const string RoutingKey = "moto.cadastrada";

        public RabbitMQMessageService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private void EnsureConnected()
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            lock (_lock)
            {
                if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                    return;

                try
                {
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(_connectionString)
                    };

                    _connection?.Dispose();
                    _channel?.Dispose();

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Declarar exchange
                    _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

                    // Declarar fila
                    _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

                    // Bind fila ao exchange
                    _channel.QueueBind(QueueName, ExchangeName, RoutingKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to RabbitMQ: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task PublishMotoCadastradaAsync(int motoId, int ano, string modelo, string placa)
        {
            try
            {
                EnsureConnected();

                var message = new
                {
                    MotoId = motoId,
                    Ano = ano,
                    Modelo = modelo,
                    Placa = placa,
                    DataCadastro = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: RoutingKey,
                    basicProperties: properties,
                    body: body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing message to RabbitMQ: {ex.Message}");
                // Não lança exceção para não quebrar o fluxo da API
            }

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

