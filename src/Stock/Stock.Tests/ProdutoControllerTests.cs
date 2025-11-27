using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stock.API.Controllers;
using Stock.API.Domain.DTOs;
using Stock.API.Domain.Entities;
using Stock.API.Domain.Interfaces;
using Stock.API.Domain.ModelViews;
using Xunit;

namespace Stock.Tests;

public class ProdutoControllerTests
{
    private readonly Mock<IProdutoService> _produtoServiceMock;
    private readonly Mock<ILogger<ProdutoController>> _loggerMock;
    private readonly ProdutoController _produtoController;

    public ProdutoControllerTests()
    {
        _produtoServiceMock = new Mock<IProdutoService>();
        _loggerMock = new Mock<ILogger<ProdutoController>>();
        _produtoController = new ProdutoController(_produtoServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllProdutos_ShouldReturnBadRequest_WhenPageIsInvalid()
    {
        // Act
        var result = await _produtoController.GetAllProdutos(page: 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O número da página deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllProdutos_ShouldReturnOk_WhenParametersAreValid()
    {
        // Arrange
        var produtos = new List<Produto> { new Produto { Id = 1, Nome = "Test" } };
        _produtoServiceMock.Setup(s => s.GetAllProdutosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(produtos);

        // Act
        var result = await _produtoController.GetAllProdutos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<ProdutoModelView>>(okResult.Value);
        Assert.Single(modelViews);
        Assert.Equal(produtos[0].Id, modelViews[0].Id);
    }

    [Fact]
    public async Task GetProdutoById_ShouldReturnNotFound_WhenProdutoDoesNotExist()
    {
        // Arrange
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Produto)null);

        // Act
        var result = await _produtoController.GetProdutoById(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetProdutoById_ShouldReturnOk_WhenProdutoExists()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Test" };
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(1))
            .ReturnsAsync(produto);

        // Act
        var result = await _produtoController.GetProdutoById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<ProdutoModelView>(okResult.Value);
        Assert.Equal(produto.Id, modelView.Id);
    }

    [Fact]
    public async Task AddProduto_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        // Arrange
        var invalidDto = new ProdutoDTO { Nome = "", Preco = -1 };

        // Act
        var result = await _produtoController.AddProduto(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.NotEmpty(errors.Messages);
        Assert.Contains("O nome do produto é obrigatório.", errors.Messages);
    }

    [Fact]
    public async Task AddProduto_ShouldReturnCreated_WhenServiceSucceeds()
    {
        // Arrange
        var validDto = new ProdutoDTO { Nome = "Test", Preco = 10, QuantidadeEstoque = 5 };
        _produtoServiceMock.Setup(s => s.AddProdutoAsync(It.IsAny<Produto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _produtoController.AddProduto(validDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProdutoController.GetProdutoById), createdResult.ActionName);
    }

    [Fact]
    public async Task UpdateProduto_ShouldReturnNotFound_WhenProdutoDoesNotExist()
    {
        // Arrange
        var validDto = new ProdutoDTO { Nome = "Test", Preco = 10 };
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Produto)null);

        // Act
        var result = await _produtoController.UpdateProduto(1, validDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProduto_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        // Arrange
        var invalidDto = new ProdutoDTO { Nome = "", Preco = -1 };

        // Act
        var result = await _produtoController.UpdateProduto(1, invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.NotEmpty(errors.Messages);
        Assert.Contains("O nome do produto é obrigatório.", errors.Messages);
    }

    [Fact]
    public async Task UpdateProduto_ShouldReturnOk_WhenServiceSucceeds()
    {
        // Arrange
        var validDto = new ProdutoDTO { Nome = "Test", Preco = 10 };
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(1))
            .ReturnsAsync(new Produto { Id = 1 });
        _produtoServiceMock.Setup(s => s.UpdateProdutoAsync(It.IsAny<Produto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _produtoController.UpdateProduto(1, validDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var produtoModelView = Assert.IsType<ProdutoModelView>(okResult.Value);
        Assert.Equal(validDto.Nome, produtoModelView.Nome);
        Assert.Equal(validDto.Preco, produtoModelView.Preco);
    }

    [Fact]
    public async Task DeleteProduto_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        // Arrange
        var produto = new Produto { Id = 1 };
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(1))
            .ReturnsAsync(produto);
        _produtoServiceMock.Setup(s => s.DeleteProdutoAsync(produto))
            .ReturnsAsync(true);

        // Act
        var result = await _produtoController.DeleteProduto(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProduto_ShouldReturnNotFound_WhenProdutoDoesNotExist()
    {
        // Arrange
        _produtoServiceMock.Setup(s => s.GetProdutoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Produto)null);

        // Act
        var result = await _produtoController.DeleteProduto(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
