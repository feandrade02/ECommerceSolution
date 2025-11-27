using Moq;
using Stock.API.Domain.Entities;
using Stock.API.Domain.Interfaces;
using Stock.API.Domain.Services;
using Xunit;

namespace Stock.Tests;

public class ProdutoServiceTests
{
    private readonly Mock<IProdutoRepository> _produtoRepositoryMock;
    private readonly ProdutoService _produtoService;

    public ProdutoServiceTests()
    {
        _produtoRepositoryMock = new Mock<IProdutoRepository>();
        _produtoService = new ProdutoService(_produtoRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        var expectedProdutos = new List<Produto> { new Produto { Id = 1, Nome = "Test" } };
        _produtoRepositoryMock.Setup(r => r.GetAllProdutosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(expectedProdutos);

        // Act
        var result = await _produtoService.GetAllProdutosAsync();

        // Assert
        Assert.Equal(expectedProdutos, result);
        _produtoRepositoryMock.Verify(r => r.GetAllProdutosAsync(
            1, 10, null, null, true, null, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // Arrange
        _produtoRepositoryMock.Setup(r => r.GetAllProdutosAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<Produto>());

        // Act
        var result = await _produtoService.GetAllProdutosAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldReturnNull_WhenParametersAreInvalid()
    {
        // Act & Assert
        Assert.Null(await _produtoService.GetAllProdutosAsync(-4, -10, " ", " ", true));
    }

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnProduto_WhenExists()
    {
        // Arrange
        var expectedProduto = new Produto { Id = 1, Nome = "Test" };
        _produtoRepositoryMock.Setup(r => r.GetProdutoByIdAsync(1))
            .ReturnsAsync(expectedProduto);

        // Act
        var result = await _produtoService.GetProdutoByIdAsync(1);

        // Assert
        Assert.Equal(expectedProduto, result);
    }

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnNull_WhenProdutoDoesNotExist()
    {
        // Arrange
        _produtoRepositoryMock.Setup(r => r.GetProdutoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Produto)null);

        // Act
        var result = await _produtoService.GetProdutoByIdAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnNull_WhenIdIsInvalid()
    {
        // Act & Assert
        Assert.Null(await _produtoService.GetProdutoByIdAsync(-1));
    }

    [Fact]
    public async Task AddProdutoAsync_ShouldCallAddAndSave_OnRepository()
    {
        // Arrange
        var produto = new Produto { Nome = "New Product" };
        _produtoRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);


        // Act
        var result = await _produtoService.AddProdutoAsync(produto);

        // Assert
        Assert.True(result);
        _produtoRepositoryMock.Verify(r => r.AddProdutoAsync(produto), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddProdutoAsync_ShouldReturnFalse_WhenProdutoIsInvalid()
    {
        // Act & Assert
        Assert.False(await _produtoService.AddProdutoAsync(null));
    }

    [Fact]
    public async Task UpdateProdutoAsync_ShouldCallUpdateAndSave_OnRepository()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Updated Product" };
        _produtoRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _produtoService.UpdateProdutoAsync(produto);

        // Assert
        Assert.True(result);
        _produtoRepositoryMock.Verify(r => r.UpdateProdutoAsync(produto), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProdutoAsync_ShouldReturnFalse_WhenProdutoIsInvalid()
    {
        // Act & Assert
        Assert.False(await _produtoService.UpdateProdutoAsync(null));
    }

    [Fact]
    public async Task DeleteProdutoAsync_ShouldCallDeleteAndSave_OnRepository()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Deleted Product" };
        _produtoRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _produtoService.DeleteProdutoAsync(produto);

        // Assert
        Assert.True(result);
        _produtoRepositoryMock.Verify(r => r.DeleteProdutoAsync(produto), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProdutoAsync_ShouldReturnFalse_WhenProdutoIsInvalid()
    {
        // Act & Assert
        Assert.False(await _produtoService.DeleteProdutoAsync(null));
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldReturnFalse_WhenProdutoNotFound()
    {
        // Arrange
        _produtoRepositoryMock.Setup(r => r.GetProdutoByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Produto)null);

        // Act
        var result = await _produtoService.UpdateStockAsync(1, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldUpdateStockAndSave_WhenProdutoExists()
    {
        // Arrange
        var produto = new Produto { Id = 1, QuantidadeEstoque = 10 };
        _produtoRepositoryMock.Setup(r => r.GetProdutoByIdAsync(1))
            .ReturnsAsync(produto);
        _produtoRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _produtoService.UpdateStockAsync(1, 5);

        // Assert
        Assert.True(result);
        Assert.Equal(15, produto.QuantidadeEstoque);
        _produtoRepositoryMock.Verify(r => r.UpdateProdutoAsync(produto), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
