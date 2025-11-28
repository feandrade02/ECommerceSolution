using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Sales.API.Controllers;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.ModelViews;
using Xunit;

namespace Sales.Tests;

public class PedidoControllerTests
{
    private readonly Mock<IPedidoService> _mockService;
    private readonly Mock<ILogger<PedidoController>> _mockLogger;
    private readonly PedidoController _controller;

    public PedidoControllerTests()
    {
        _mockService = new Mock<IPedidoService>();
        _mockLogger = new Mock<ILogger<PedidoController>>();
        _controller = new PedidoController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAllPedidos Tests

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenPageIsInvalid()
    {
        // Act
        var result = await _controller.GetAllPedidos(page: 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O número da página deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Act
        var result = await _controller.GetAllPedidos(pageSize: 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O tamanho da página deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenSortByIsInvalid()
    {
        // Act
        var result = await _controller.GetAllPedidos(sortBy: "invalid");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O campo de ordenação deve ser 'valortotal' ou vazio.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenMinTotalValueIsNegative()
    {
        // Act
        var result = await _controller.GetAllPedidos(minTotalValue: -1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O valor total mínimo deve ser maior ou igual a zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenMaxTotalValueIsNegative()
    {
        // Act
        var result = await _controller.GetAllPedidos(maxTotalValue: -1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O valor total máximo deve ser maior ou igual a zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnBadRequest_WhenMinGreaterThanMax()
    {
        // Act
        var result = await _controller.GetAllPedidos(minTotalValue: 100, maxTotalValue: 50);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O valor total mínimo não pode ser maior que o valor total máximo.", errors.Messages);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnOk_WithValidSortBy()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new Pedido
            {
                Id = 1,
                IdCliente = 1,
                ValorTotal = 100,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>()
            }
        };
        _mockService.Setup(s => s.GetAllPedidosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pedidos);

        // Act
        var result = await _controller.GetAllPedidos(sortBy: "valortotal");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<PedidoModelView>>(okResult.Value);
        Assert.Single(modelViews);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnOk_WhenParametersAreValid()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new Pedido
            {
                Id = 1,
                IdCliente = 1,
                ValorTotal = 100,
                Status = StatusPedido.Confirmado,
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { IdProduto = 1, NomeProduto = "Produto A", PrecoUnitario = 50, Quantidade = 2 }
                }
            }
        };
        _mockService.Setup(s => s.GetAllPedidosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pedidos);

        // Act
        var result = await _controller.GetAllPedidos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<PedidoModelView>>(okResult.Value);
        Assert.Single(modelViews);
        Assert.Equal(pedidos[0].Id, modelViews[0].Id);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnEmptyList_WhenNoDataExists()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllPedidosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<Pedido>());

        // Act
        var result = await _controller.GetAllPedidos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<PedidoModelView>>(okResult.Value);
        Assert.Empty(modelViews);
    }

    [Fact]
    public async Task GetAllPedidos_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllPedidosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetAllPedidos();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetPedidoById Tests

    [Fact]
    public async Task GetPedidoById_ShouldReturnNotFound_WhenPedidoDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetPedidoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _controller.GetPedidoById(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPedidoById_ShouldReturnOk_WhenPedidoExists()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, NomeProduto = "Produto A", PrecoUnitario = 50, Quantidade = 2 }
            }
        };
        _mockService.Setup(s => s.GetPedidoByIdAsync(1))
            .ReturnsAsync(pedido);

        // Act
        var result = await _controller.GetPedidoById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<PedidoModelView>(okResult.Value);
        Assert.Equal(pedido.Id, modelView.Id);
    }

    [Fact]
    public async Task GetPedidoById_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        _mockService.Setup(s => s.GetPedidoByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetPedidoById(1);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region AddPedido Tests

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenDtoIsNull()
    {
        // Act
        var result = await _controller.AddPedido(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O pedido não pode ser vazio ou nulo.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenItensIsNull()
    {
        // Arrange
        var invalidDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = null!
        };

        // Act
        var result = await _controller.AddPedido(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O pedido deve conter pelo menos um item.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenIdClienteIsInvalid()
    {
        // Arrange
        var invalidDto = new PedidoDTO
        {
            IdCliente = 0,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 1 } }
        };

        // Act
        var result = await _controller.AddPedido(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O ID do cliente é obrigatório e deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenItensIsEmpty()
    {
        // Arrange
        var invalidDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO>()
        };

        // Act
        var result = await _controller.AddPedido(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O pedido deve conter pelo menos um item.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenIdProdutoIsInvalid()
    {
        // Arrange
        var invalidDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 0, Quantidade = 1 } }
        };

        // Act
        var result = await _controller.AddPedido(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O ID do produto é obrigatório e deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnBadRequest_WhenQuantidadeIsInvalid()
    {
        // Arrange
        var invalidDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 0 } }
        };

        // Act
        var result = await _controller.AddPedido(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("A quantidade do produto deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnCreated_WhenDtoIsValid()
    {
        // Arrange
        var validDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 2 } }
        };

        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, NomeProduto = "Produto A", PrecoUnitario = 50, Quantidade = 2 }
            }
        };

        _mockService.Setup(s => s.CreatePedidoFromDTOAsync(It.IsAny<PedidoDTO>()))
            .ReturnsAsync(pedido);
        _mockService.Setup(s => s.AddPedidoAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AddPedido(validDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PedidoController.GetPedidoById), createdResult.ActionName);
        var modelView = Assert.IsType<PedidoModelView>(createdResult.Value);
        Assert.Equal(pedido.Id, modelView.Id);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnNotFound_WhenProductNotFound()
    {
        // Arrange
        var validDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 999, Quantidade = 1 } }
        };

        _mockService.Setup(s => s.CreatePedidoFromDTOAsync(It.IsAny<PedidoDTO>()))
            .ThrowsAsync(new KeyNotFoundException("Produto não encontrado"));

        // Act
        var result = await _controller.AddPedido(validDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("não encontrado", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task AddPedido_ShouldReturnConflict_WhenStockInsufficient()
    {
        // Arrange
        var validDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 100 } }
        };

        _mockService.Setup(s => s.CreatePedidoFromDTOAsync(It.IsAny<PedidoDTO>()))
            .ThrowsAsync(new InvalidOperationException("Estoque insuficiente"));

        // Act
        var result = await _controller.AddPedido(validDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Estoque insuficiente", conflictResult.Value?.ToString());
    }

    [Fact]
    public async Task AddPedido_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        var validDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 1 } }
        };

        _mockService.Setup(s => s.CreatePedidoFromDTOAsync(It.IsAny<PedidoDTO>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.AddPedido(validDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task AddPedido_ShouldReturnInternalServerError_WhenSaveFails()
    {
        // Arrange
        var validDto = new PedidoDTO
        {
            IdCliente = 1,
            Itens = new List<ItemPedidoDTO> { new ItemPedidoDTO { IdProduto = 1, Quantidade = 2 } }
        };

        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>()
        };

        _mockService.Setup(s => s.CreatePedidoFromDTOAsync(It.IsAny<PedidoDTO>()))
            .ReturnsAsync(pedido);
        _mockService.Setup(s => s.AddPedidoAsync(It.IsAny<Pedido>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AddPedido(validDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region DeletePedido Tests

    [Fact]
    public async Task DeletePedido_ShouldReturnNotFound_WhenPedidoDoesNotExist()
    {
        // Arrange
        _mockService.Setup(s => s.GetPedidoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Pedido?)null);

        // Act
        var result = await _controller.DeletePedido(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePedido_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>()
        };

        _mockService.Setup(s => s.GetPedidoByIdAsync(1))
            .ReturnsAsync(pedido);
        _mockService.Setup(s => s.DeletePedidoAsync(pedido))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePedido(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePedido_ShouldReturnInternalServerError_WhenServiceThrows()
    {
        // Arrange
        var pedido = new Pedido { Id = 1, Itens = new List<ItemPedido>() };
        _mockService.Setup(s => s.GetPedidoByIdAsync(1))
            .ReturnsAsync(pedido);
        _mockService.Setup(s => s.DeletePedidoAsync(It.IsAny<Pedido>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.DeletePedido(1);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeletePedido_ShouldReturnInternalServerError_WhenDeleteFails()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = 1,
            IdCliente = 1,
            ValorTotal = 100,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>()
        };

        _mockService.Setup(s => s.GetPedidoByIdAsync(1))
            .ReturnsAsync(pedido);
        _mockService.Setup(s => s.DeletePedidoAsync(pedido))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeletePedido(1);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion
}
