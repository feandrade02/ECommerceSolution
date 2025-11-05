using System.Text;
using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Stock.API.Domain.Interfaces;

namespace Stock.API.Workers;

public class UpdateStockWorker : IHostedService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateStockWorker> _logger;
    private IChannel _channel;
    private const string QueueName = "update_stock_queue";

    public UpdateStockWorker(IConnection connection, IServiceProvider serviceProvider, ILogger<UpdateStockWorker> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Cria um canal para comunicação com o RabbitMQ
        _channel = await _connection.CreateChannelAsync();
        
        await _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false,
            autoDelete: false, arguments: null);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        _logger.LogInformation("Worker de UpdateStockEvent iniciado. Aguardando mensagens na fila '{QueueName}'.", QueueName);
        
        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        var body = eventArgs.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        _logger.LogInformation("Mensagem recebida: {Message}", message);

        try
        {
            var updateStockEvent = JsonSerializer.Deserialize<UpdateStockEvent>(message);

            if (updateStockEvent?.Itens != null)
            {
                // Criamos um escopo para resolver o IProdutoService, que é Scoped.
                using var scope = _serviceProvider.CreateScope();
                var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();

                foreach (var item in updateStockEvent.Itens)
                {
                    await produtoService.UpdateStockAsync(item.IdProduto, item.Quantidade);
                }

                // Confirma o processamento da mensagem para o RabbitMQ, que a removerá da fila.
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                _logger.LogInformation("Estoque atualizado para o evento com CorrelationId: {CorrelationId}", updateStockEvent.CorrelationId);
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Erro ao desserializar a mensagem. Conteúdo: {Message}", message);
            // Rejeita a mensagem, mas não a re-enfileira para evitar loops de erro.
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar a mensagem do PedidoCriadoEvent.");
            // Rejeita a mensagem, mas não a re-enfileira.
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker de PedidoCriadoEvent sendo finalizado.");
        await _channel.CloseAsync();
    }
}
