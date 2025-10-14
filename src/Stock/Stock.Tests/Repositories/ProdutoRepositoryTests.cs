using Microsoft.EntityFrameworkCore;
using Stock.API.Repositories;
using Stock.Context;
using Stock.Domain.Entities;

namespace Stock.Tests.Repositories;

public class ProdutoRepositoryTests : IDisposable
{
    private readonly StockContext _context;
    private readonly ProdutoRepository _repository;

    public ProdutoRepositoryTests()
    {
        // Configura o DbContext para usar um banco de dados em memória
        var options = new DbContextOptionsBuilder<StockContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Guid garante um BD novo para cada teste
            .Options;

        _context = new StockContext(options);
        _repository = new ProdutoRepository(_context);
    }

    [Fact]
    public async Task AddProduct_ShouldAddProductAndSetTimestamps()
    {
        // Arrange
        var produto = new Produto { Nome = "Produto Novo", Preco = 50, QuantidadeEstoque = 10 };

        // Act
        await _repository.AddProdutoAsync(produto);
        await _repository.SaveChangesAsync();

        // Assert
        var produtoAdicionado = await _context.Produtos.FirstOrDefaultAsync(p => p.Nome == "Produto Novo");
        Assert.NotNull(produtoAdicionado);
        Assert.NotEqual(default(DateTime), produtoAdicionado.CreatedAt);
        Assert.NotEqual(default(DateTime), produtoAdicionado.UpdatedAt);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnCorrectProduct_WhenNotDeleted()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Produto Existente" };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProdutoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Produto Existente", result.Nome);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnNull_WhenDeleted()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Produto Deletado", IsDeleted = true };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProdutoByIdAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllProducts_ShouldFilterAndPaginateCorrectly()
    {
        // Arrange
        var produtos = new List<Produto>
        {
            new Produto { Nome = "Caneta Azul", Preco = 2, QuantidadeEstoque = 100 },
            new Produto { Nome = "Caneta Preta", Preco = 2, QuantidadeEstoque = 50 },
            new Produto { Nome = "Lápis", Preco = 1, QuantidadeEstoque = 200 },
            new Produto { Nome = "Caderno", Preco = 15, QuantidadeEstoque = 20 },
            new Produto { Nome = "Borracha", Preco = 3, QuantidadeEstoque = 80, IsDeleted = true }
        };
        _context.Produtos.AddRange(produtos);
        await _context.SaveChangesAsync();

        // Act
        // Busca por "Caneta", com preço máximo de 10, ordenado por nome, na primeira página
        var result = await _repository.GetAllProdutosAsync(
            page: 1,
            pageSize: 5,
            name: "Caneta",
            ascending: true,
            maxPrice: 10
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Deve encontrar "Caneta Azul" e "Caneta Preta"
        Assert.Equal("Caneta Azul", result[0].Nome); // Ordenado ascendentemente
        Assert.Equal("Caneta Preta", result[1].Nome);
        Assert.DoesNotContain(result, p => p.Nome == "Lápis"); // Não corresponde ao filtro de nome
        Assert.DoesNotContain(result, p => p.Nome == "Caderno"); // Não corresponde ao filtro de preço
        Assert.DoesNotContain(result, p => p.Nome == "Borracha"); // Foi deletado
    }

    [Fact]
    public async Task DeleteProduct_ShouldPerformSoftDelete()
    {
        // Arrange
        var produto = new Produto { Id = 1, Nome = "Produto para Deletar" };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteProdutoAsync(produto);
        await _repository.SaveChangesAsync();

        // Assert
        var produtoNoDb = await _context.Produtos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == 1);
        Assert.NotNull(produtoNoDb);
        Assert.True(produtoNoDb.IsDeleted);
        Assert.NotNull(produtoNoDb.DeletedAt);

        // Verifica que o método normal de busca não o encontra mais
        var produtoVisivel = await _repository.GetProdutoByIdAsync(1);
        Assert.Null(produtoVisivel);
    }

    [Fact]
    public async Task UpdateProduct_ShouldUpdateProductDetailsAndTimestamp()
    {
        // Arrange
        var originalProduct = new Produto { Id = 1, Nome = "Produto Original", Preco = 100 };
        _context.Produtos.Add(originalProduct);
        await _context.SaveChangesAsync();
        var originalUpdatedAt = originalProduct.UpdatedAt;

        // Simula um cenário desconectado, onde a entidade é adicionada e depois enviada para atualização.
        _context.Entry(originalProduct).State = EntityState.Detached;

        var updatedProductData = new Produto
        {
            Id = 1,
            Nome = "Produto Atualizado",
            Preco = 150
        };

        // Act
        await _repository.UpdateProdutoAsync(updatedProductData);
        await _repository.SaveChangesAsync();

        // Assert
        var productFromDb = await _context.Produtos.FindAsync(1);
        Assert.Equal("Produto Atualizado", productFromDb.Nome);
        Assert.Equal(150, productFromDb.Preco);
        Assert.True(productFromDb.UpdatedAt > originalUpdatedAt);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
