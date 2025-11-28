using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Stock.API.Domain.Interfaces;
using Stock.API.Workers;
using Xunit;

namespace Stock.Tests;

public class UpdateStockWorkerTests
{
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IChannel> _mockChannel;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IProdutoService> _mockProdutoService;
    private readonly Mock<ILogger<UpdateStockWorker>> _mockLogger;
    private readonly UpdateStockWorker _worker;

    public UpdateStockWorkerTests()
    {
        _mockConnection = new Mock<IConnection>();
        _mockChannel = new Mock<IChannel>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockProdutoService = new Mock<IProdutoService>();
        _mockLogger = new Mock<ILogger<UpdateStockWorker>>();

        // Setup do ServiceProvider para retornar o ProdutoService diretamente
        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(s => s.ServiceProvider.GetService(typeof(IProdutoService)))
            .Returns(_mockProdutoService.Object);
        
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        serviceScopeFactory.Setup(f => f.CreateScope())
            .Returns(serviceScope.Object);
        
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);

        // Setup do Connection para retornar o Channel
        _mockConnection.SetReturnsDefault(Task.FromResult(_mockChannel.Object));

        _worker = new UpdateStockWorker(
            _mockConnection.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object);
    }

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldNotThrowException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await _worker.StartAsync(cancellationToken);
        // Se chegou aqui sem exceção, o teste passou
    }

    #endregion

    #region OnMessageReceived Tests

    [Fact]
    public async Task OnMessageReceived_ShouldProcessValidMessage_AndUpdateStock()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>
            {
                new ItemPedidoReference { IdProduto = 1, Quantidade = 5 },
                new ItemPedidoReference { IdProduto = 2, Quantidade = 10 }
            }
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 1);

        _mockProdutoService.Setup(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(1, 5), Times.Once);
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(2, 10), Times.Once);
        _mockChannel.Verify(ch => ch.BasicAckAsync(1, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldHandleInvalidJson_AndSendNack()
    {
        // Arrange
        var invalidMessage = "{ invalid json }";
        var body = Encoding.UTF8.GetBytes(invalidMessage);
        var eventArgs = CreateBasicDeliverEventArgs(body, 2);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockChannel.Verify(ch => ch.BasicNackAsync(2, false, false, It.IsAny<CancellationToken>()), Times.Once);
        _mockChannel.Verify(ch => ch.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldHandleServiceException_AndSendNack()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>
            {
                new ItemPedidoReference { IdProduto = 1, Quantidade = 5 }
            }
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 3);

        _mockProdutoService.Setup(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockChannel.Verify(ch => ch.BasicNackAsync(3, false, false, It.IsAny<CancellationToken>()), Times.Once);
        _mockChannel.Verify(ch => ch.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldHandleNullEvent_AndNotUpdateStock()
    {
        // Arrange
        var message = "null";
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 4);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        // Não deve fazer ACK nem NACK porque o evento é nulo mas não é um erro
        _mockChannel.Verify(ch => ch.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(ch => ch.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldHandleEmptyItens_AndNotUpdateStock()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>() // Lista vazia
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 5);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _mockChannel.Verify(ch => ch.BasicAckAsync(5, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldHandleNullItens_AndNotUpdateStock()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = null! // Itens nulo
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 6);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockProdutoService.Verify(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        // Não deve fazer ACK nem NACK porque Itens é nulo
        _mockChannel.Verify(ch => ch.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(ch => ch.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldProcessMultipleItems_InCorrectOrder()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>
            {
                new ItemPedidoReference { IdProduto = 1, Quantidade = 5 },
                new ItemPedidoReference { IdProduto = 2, Quantidade = 10 },
                new ItemPedidoReference { IdProduto = 3, Quantidade = 15 }
            }
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 7);

        var callOrder = new List<(int productId, int quantity)>();
        _mockProdutoService.Setup(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((id, qty) => callOrder.Add((id, qty)))
            .ReturnsAsync(true);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        Assert.Equal(3, callOrder.Count);
        Assert.Equal((1, 5), callOrder[0]);
        Assert.Equal((2, 10), callOrder[1]);
        Assert.Equal((3, 15), callOrder[2]);
        _mockChannel.Verify(ch => ch.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldLogInformation_WhenProcessingMessage()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>
            {
                new ItemPedidoReference { IdProduto = 1, Quantidade = 5 }
            }
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 8);

        _mockProdutoService.Setup(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Mensagem recebida")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Estoque atualizado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldLogError_WhenJsonExceptionOccurs()
    {
        // Arrange
        var invalidMessage = "{ invalid json }";
        var body = Encoding.UTF8.GetBytes(invalidMessage);
        var eventArgs = CreateBasicDeliverEventArgs(body, 9);

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro ao desserializar")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnMessageReceived_ShouldLogError_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var updateStockEvent = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = new List<ItemPedidoReference>
            {
                new ItemPedidoReference { IdProduto = 1, Quantidade = 5 }
            }
        };

        var message = JsonSerializer.Serialize(updateStockEvent);
        var body = Encoding.UTF8.GetBytes(message);
        var eventArgs = CreateBasicDeliverEventArgs(body, 10);

        _mockProdutoService.Setup(ps => ps.UpdateStockAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        await _worker.StartAsync(CancellationToken.None);

        // Act
        await _worker.OnMessageReceived(null, eventArgs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Erro inesperado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldNotThrowException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await _worker.StartAsync(cancellationToken);

        // Act & Assert
        await _worker.StopAsync(cancellationToken);
        // Se chegou aqui sem exceção, o teste passou
    }

    [Fact]
    public async Task StopAsync_ShouldLogInformation()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        await _worker.StartAsync(cancellationToken);

        // Act
        await _worker.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("sendo finalizado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private BasicDeliverEventArgs CreateBasicDeliverEventArgs(byte[] body, ulong deliveryTag)
    {
        return new BasicDeliverEventArgs(
            consumerTag: "test-consumer",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "test-exchange",
            routingKey: "test-routing-key",
            properties: null!,
            body: new ReadOnlyMemory<byte>(body),
            cancellationToken: CancellationToken.None
        );
    }

    #endregion
}
