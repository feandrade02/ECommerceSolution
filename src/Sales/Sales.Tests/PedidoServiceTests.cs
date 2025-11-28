using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RabbitMQ.Client;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.Services;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Sales.Tests;

public class PedidoServiceTests
{
    private readonly Mock<IPedidoRepository> _mockRepository;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConnection> _mockRabbitConnection;
    private readonly Mock<ILogger<PedidoService>> _mockLogger;
    private readonly PedidoService _service;

    public PedidoServiceTests()
    {
        _mockRepository = new Mock<IPedidoRepository>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockRabbitConnection = new Mock<IConnection>();
        _mockLogger = new Mock<ILogger<PedidoService>>();

        _service = new PedidoService(
            _mockRepository.Object,
            _mockHttpClientFactory.Object,
            _mockLogger.Object,
            _mockRabbitConnection.Object
        );
    }

    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode, object content)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(content)
            });
        return mockHandler;
    }

    #region GetAllPedidosAsync Tests

    [Fact]
    public async Task GetAllPedidosAsync_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        var pedidos = new List<Pedido>();
        _mockRepository
            .Setup(r => r.GetAllPedidosAsync(1, 10, "valortotal", true, 100, 500))
            .ReturnsAsync(pedidos);

        // Act
        await _service.GetAllPedidosAsync(1, 10, "valortotal", true, 100, 500);

        // Assert
        _mockRepository.Verify(
            r => r.GetAllPedidosAsync(1, 10, "valortotal", true, 100, 500),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldReturnPedidos_WhenRepositoryReturnsData()
    {
        // Arrange
        var expectedPedidos = new List<Pedido>
        {
            new Pedido { Id = 1, IdCliente = 1, ValorTotal = 100 },
            new Pedido { Id = 2, IdCliente = 2, ValorTotal = 200 }
        };
        _mockRepository
            .Setup(r => r.GetAllPedidosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(expectedPedidos);

        // Act
        var result = await _service.GetAllPedidosAsync(1, 10, null, true, null, null);

        // Assert
        Assert.Equal(expectedPedidos, result);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllPedidosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        var result = await _service.GetAllPedidosAsync(1, 10, null, true, null, null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async task GetAllPedidosAsync_ShouldReturnNull_WhenParametersAreInvalid()
    {
        // Act & Assert
        Assert.Null(await _service.GetAllPedidosAsync(-1, -5, "", true, -2, -7));
    }

    #endregion

    #region GetPedidoByIdAsync Tests

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldCallRepository_WithCorrectId()
    {
        // Arrange
        var pedido = new Pedido { Id = 1 };
        _mockRepository.Setup(r => r.GetPedidoByIdAsync(1)).ReturnsAsync(pedido);

        // Act
        await _service.GetPedidoByIdAsync(1);

        // Assert
        _mockRepository.Verify(r => r.GetPedidoByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldReturnPedido_WhenPedidoExists()
    {
        // Arrange
        var expectedPedido = new Pedido { Id = 1, IdCliente = 1, ValorTotal = 100 };
        _mockRepository.Setup(r => r.GetPedidoByIdAsync(1)).ReturnsAsync(expectedPedido);

        // Act
        var result = await _service.GetPedidoByIdAsync(1);

        // Assert
        Assert.Equal(expectedPedido, result);
    }

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldReturnNull_WhenPedidoDoesNotExist()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPedidoByIdAsync(999)).ReturnsAsync((Pedido?)null);

        // Act
        var result = await _service.GetPedidoByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreatePedidoFromDTOAsync Tests

    [Fact]
    public async Task CreatePedidoFromDTOAsync_ShouldCreatePedido_WhenStockIsAvailable()
    {
        // Arrange
        var pedidoDTO = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>
            {
                new ItemPedidoDTO { IdProduto = 1, Quantidade = 2 }
            }
        };

        var produtoInfo = new ProdutoInfoDTO
        {
            Nome = "Produto A",
            Preco = 50.00m,
            QuantidadeEstoque = 10
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, produtoInfo);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("StockAPI")).Returns(httpClient);

        // Act
        var result = await _service.CreatePedidoFromDTOAsync(pedidoDTO);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.IdCliente);
        Assert.Equal(StatusPedido.Confirmado, result.Status);
        Assert.Single(result.Itens);
        Assert.Equal("Produto A", result.Itens[0].NomeProduto);
    }

    [Fact]
    public async Task CreatePedidoFromDTOAsync_ShouldCalculateValorTotal_Correctly()
    {
        // Arrange
        var pedidoDTO = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>
            {
                new ItemPedidoDTO { IdProduto = 1, Quantidade = 2 },
                new ItemPedidoDTO { IdProduto = 2, Quantidade = 3 }
            }
        };

        var produto1 = new ProdutoInfoDTO { Nome = "Produto A", Preco = 50.00m, QuantidadeEstoque = 10 };
        var produto2 = new ProdutoInfoDTO { Nome = "Produto B", Preco = 30.00m, QuantidadeEstoque = 10 };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(produto1)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(produto2)
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("StockAPI")).Returns(httpClient);

        // Act
        var result = await _service.CreatePedidoFromDTOAsync(pedidoDTO);

        // Assert
        Assert.Equal(190.00m, result.ValorTotal); // (2 * 50) + (3 * 30) = 100 + 90 = 190
    }

    [Fact]
    public async Task CreatePedidoFromDTOAsync_ShouldThrowException_WhenStockInsufficient()
    {
        // Arrange
        var pedidoDTO = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>
            {
                new ItemPedidoDTO { IdProduto = 1, Quantidade = 20 }
            }
        };

        var produtoInfo = new ProdutoInfoDTO
        {
            Nome = "Produto A",
            Preco = 50.00m,
            QuantidadeEstoque = 5 // Estoque insuficiente
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, produtoInfo);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("StockAPI")).Returns(httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePedidoFromDTOAsync(pedidoDTO)
        );
        Assert.Contains("Estoque insuficiente", exception.Message);
    }

    [Fact]
    public async Task CreatePedidoFromDTOAsync_ShouldThrowException_WhenProductNotFound()
    {
        // Arrange
        var pedidoDTO = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>
            {
                new ItemPedidoDTO { IdProduto = 999, Quantidade = 1 }
            }
        };

        var mockHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, (ProdutoInfoDTO?)null);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("StockAPI")).Returns(httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreatePedidoFromDTOAsync(pedidoDTO)
        );
        Assert.Contains("não encontrado", exception.Message);
    }

    [Fact]
    public async Task CreatePedidoFromDTOAsync_ShouldThrowException_WhenHttpRequestFails()
    {
        // Arrange
        var pedidoDTO = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>
            {
                new ItemPedidoDTO { IdProduto = 1, Quantidade = 1 }
            }
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("StockAPI")).Returns(httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePedidoFromDTOAsync(pedidoDTO)
        );
        Assert.Contains("Não foi possível obter informações do produto", exception.Message);
    }

    #endregion

    #region AddPedidoAsync Tests

    [Fact]
    public async Task AddPedidoAsync_ShouldCallRepository_AndReturnTrue()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, Quantidade = 2 }
            }
        };

        _mockRepository.Setup(r => r.AddPedidoAsync(It.IsAny<Pedido>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.AddPedidoAsync(pedido);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.AddPedidoAsync(pedido), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddPedidoAsync_ShouldThrowException_WhenDbUpdateFails()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, Quantidade = 2 }
            }
        };

        _mockRepository.Setup(r => r.AddPedidoAsync(It.IsAny<Pedido>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("DB Error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddPedidoAsync(pedido)
        );
        Assert.Contains("banco de dados", exception.Message);
    }

    #endregion

    #region UpdatePedidoAsync Tests

    [Fact]
    public async Task UpdatePedidoAsync_ShouldCallRepository_WithCorrectPedido()
    {
        // Arrange
        var pedido = new Pedido { Id = 1, IdCliente = 1, ValorTotal = 150 };
        _mockRepository.Setup(r => r.UpdatePedidoAsync(It.IsAny<Pedido>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        await _service.UpdatePedidoAsync(pedido);

        // Assert
        _mockRepository.Verify(r => r.UpdatePedidoAsync(pedido), Times.Once);
    }

    [Fact]
    public async Task UpdatePedidoAsync_ShouldReturnTrue_WhenSaveSucceeds()
    {
        // Arrange
        var pedido = new Pedido { Id = 1, IdCliente = 1, ValorTotal = 150 };
        _mockRepository.Setup(r => r.UpdatePedidoAsync(It.IsAny<Pedido>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.UpdatePedidoAsync(pedido);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePedidoAsync_ShouldReturnFalse_WhenSaveFails()
    {
        // Arrange
        var pedido = new Pedido { Id = 1, IdCliente = 1, ValorTotal = 150 };
        _mockRepository.Setup(r => r.UpdatePedidoAsync(It.IsAny<Pedido>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await _service.UpdatePedidoAsync(pedido);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeletePedidoAsync Tests

    [Fact]
    public async Task DeletePedidoAsync_ShouldCallRepository_AndReturnTrue()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, Quantidade = 2, IsDeleted = false }
            }
        };

        _mockRepository.Setup(r => r.DeletePedidoAsync(It.IsAny<Pedido>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.DeletePedidoAsync(pedido);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeletePedidoAsync(pedido), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePedidoAsync_ShouldThrowException_WhenDbUpdateFails()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, Quantidade = 2, IsDeleted = false }
            }
        };

        _mockRepository.Setup(r => r.DeletePedidoAsync(It.IsAny<Pedido>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("DB Error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeletePedidoAsync(pedido)
        );
        Assert.Contains("banco de dados", exception.Message);
    }

    #endregion
}
