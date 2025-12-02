using Microsoft.EntityFrameworkCore;
using Stock.API.Context;
using Stock.API.Domain.Entities;
using Stock.API.Repositories;
using Xunit;

namespace Stock.Tests;

public class ProdutoRepositoryTests
{
    private StockContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StockContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new StockContext(options);
    }

    private async Task<StockContext> CreateContextWithData()
    {
        var context = CreateContext();

        var produtos = new List<Produto>
        {
            new Produto { Id = 1, Nome = "Produto A", Descricao = "Descrição A", Preco = 10.50m, QuantidadeEstoque = 100, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Produto { Id = 2, Nome = "Produto B", Descricao = "Descrição B", Preco = 20.00m, QuantidadeEstoque = 50, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Produto { Id = 3, Nome = "Produto C", Descricao = "Descrição C", Preco = 30.75m, QuantidadeEstoque = 25, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Produto { Id = 4, Nome = "Item X", Descricao = "Descrição X", Preco = 15.00m, QuantidadeEstoque = 75, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Produto { Id = 5, Nome = "Produto Deletado", Descricao = "Descrição D", Preco = 40.00m, QuantidadeEstoque = 10, IsDeleted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, DeletedAt = DateTime.UtcNow }
        };

        await context.Produtos.AddRangeAsync(produtos);
        await context.SaveChangesAsync();

        return context;
    }

    #region GetAllProdutosAsync Tests

    [Fact]
    public async Task GetAllProdutosAsync_ShouldReturnAllProducts_WhenNoFiltersApplied()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync();

        // Assert
        Assert.Equal(4, result.Count); // 4 produtos não deletados
        Assert.DoesNotContain(result, p => p.IsDeleted);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldFilterByName_WhenNameProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(name: "Produto");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.Contains("Produto", p.Nome));
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldFilterByPriceRange_WhenMinAndMaxPriceProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(minPrice: 15, maxPrice: 25);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.InRange(p.Preco, 15, 25));
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldFilterByStockRange_WhenMinAndMaxStockProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(minStock: 50, maxStock: 100);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.InRange(p.QuantidadeEstoque, 50, 100));
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldSortByNomeAscending_WhenSortByNomeAndAscendingTrue()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(sortBy: "nome", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("Item X", result[0].Nome);
        Assert.Equal("Produto A", result[1].Nome);
        Assert.Equal("Produto B", result[2].Nome);
        Assert.Equal("Produto C", result[3].Nome);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldSortByPrecoDescending_WhenSortByPrecoAndAscendingFalse()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(sortBy: "preco", ascending: false);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(30.75m, result[0].Preco);
        Assert.Equal(20.00m, result[1].Preco);
        Assert.Equal(15.00m, result[2].Preco);
        Assert.Equal(10.50m, result[3].Preco);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldSortByQuantidadeEstoqueAscending_WhenSortByQuantidadeEstoque()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync(sortBy: "quantidadeestoque", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(25, result[0].QuantidadeEstoque);
        Assert.Equal(50, result[1].QuantidadeEstoque);
        Assert.Equal(75, result[2].QuantidadeEstoque);
        Assert.Equal(100, result[3].QuantidadeEstoque);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldApplyPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var page1 = await repository.GetAllProdutosAsync(page: 1, pageSize: 2);
        var page2 = await repository.GetAllProdutosAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task GetAllProdutosAsync_ShouldExcludeDeletedProducts_Always()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetAllProdutosAsync();

        // Assert
        Assert.DoesNotContain(result, p => p.IsDeleted);
        Assert.DoesNotContain(result, p => p.Nome == "Produto Deletado");
    }

    #endregion

    #region GetProdutoByIdAsync Tests

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnProduto_WhenProdutoExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetProdutoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Produto A", result.Nome);
    }

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnNull_WhenProdutoDoesNotExist()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetProdutoByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProdutoByIdAsync_ShouldReturnNull_WhenProdutoIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.GetProdutoByIdAsync(5); // Produto Deletado

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddProdutoAsync Tests

    [Fact]
    public async Task AddProdutoAsync_ShouldAddProduto_Successfully()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ProdutoRepository(context);
        var produto = new Produto
        {
            Nome = "Novo Produto",
            Descricao = "Nova Descrição",
            Preco = 50.00m,
            QuantidadeEstoque = 200
        };

        // Act
        await repository.AddProdutoAsync(produto);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Produtos.FindAsync(produto.Id);
        Assert.NotNull(result);
        Assert.Equal("Novo Produto", result.Nome);
    }

    [Fact]
    public async Task AddProdutoAsync_ShouldSetCreatedAtAndUpdatedAt_WhenAdding()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ProdutoRepository(context);
        var beforeAdd = DateTime.UtcNow;
        var produto = new Produto
        {
            Nome = "Novo Produto",
            Descricao = "Nova Descrição",
            Preco = 50.00m,
            QuantidadeEstoque = 200
        };

        // Act
        await repository.AddProdutoAsync(produto);
        await repository.SaveChangesAsync();
        var afterAdd = DateTime.UtcNow;

        // Assert
        var result = await context.Produtos.FindAsync(produto.Id);
        Assert.NotNull(result);
        Assert.InRange(result.CreatedAt, beforeAdd, afterAdd);
        Assert.InRange(result.UpdatedAt, beforeAdd, afterAdd);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    #endregion

    #region UpdateProdutoAsync Tests

    [Fact]
    public async Task UpdateProdutoAsync_ShouldUpdateProduto_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);
        var produto = await repository.GetProdutoByIdAsync(1);
        produto.Nome = "Produto A Atualizado";
        produto.Preco = 99.99m;

        // Act
        await repository.UpdateProdutoAsync(produto);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Produtos.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Produto A Atualizado", result.Nome);
        Assert.Equal(99.99m, result.Preco);
    }

    [Fact]
    public async Task UpdateProdutoAsync_ShouldUpdateUpdatedAt_WhenUpdating()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);
        var produto = await repository.GetProdutoByIdAsync(1);
        var originalUpdatedAt = produto.UpdatedAt;
        
        // Aguarda um tempo para garantir que o UpdatedAt seja diferente
        await Task.Delay(10);
        
        produto.Nome = "Produto A Atualizado";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await repository.UpdateProdutoAsync(produto);
        await repository.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var result = await context.Produtos.FindAsync(1);
        Assert.NotNull(result);
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
        Assert.NotEqual(originalUpdatedAt, result.UpdatedAt);
    }

    #endregion

    #region DeleteProdutoAsync Tests

    [Fact]
    public async Task DeleteProdutoAsync_ShouldMarkAsDeleted_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);
        var produto = await repository.GetProdutoByIdAsync(1);

        // Act
        await repository.DeleteProdutoAsync(produto);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Produtos.FindAsync(1);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeleteProdutoAsync_ShouldSetDeletedAt_WhenDeleting()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);
        var produto = await repository.GetProdutoByIdAsync(1);
        var beforeDelete = DateTime.UtcNow;

        // Act
        await repository.DeleteProdutoAsync(produto);
        await repository.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var result = await context.Produtos.FindAsync(1);
        Assert.NotNull(result);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete, afterDelete);
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenChangesAreSaved()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ProdutoRepository(context);
        var produto = new Produto
        {
            Nome = "Novo Produto",
            Descricao = "Nova Descrição",
            Preco = 50.00m,
            QuantidadeEstoque = 200
        };
        await repository.AddProdutoAsync(produto);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenNoChangesToSave()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ProdutoRepository(context);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.False(result);
    }

    #endregion
}
