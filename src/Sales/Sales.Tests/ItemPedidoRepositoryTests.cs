using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Entities;
using Sales.API.Repositories;
using Xunit;

namespace Sales.Tests;

public class ItemPedidoRepositoryTests
{
    private SalesContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SalesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SalesContext(options);
    }

    private async Task<SalesContext> CreateContextWithData()
    {
        var context = CreateContext();

        var items = new List<ItemPedido>
        {
            new ItemPedido
            {
                Id = 1,
                IdProduto = 1,
                PedidoId = 1,
                NomeProduto = "Produto A",
                PrecoUnitario = 50.00m,
                Quantidade = 2,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new ItemPedido
            {
                Id = 2,
                IdProduto = 2,
                PedidoId = 1,
                NomeProduto = "Produto B",
                PrecoUnitario = 100.00m,
                Quantidade = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-4)
            },
            new ItemPedido
            {
                Id = 3,
                IdProduto = 3,
                PedidoId = 2,
                NomeProduto = "Produto C",
                PrecoUnitario = 25.00m,
                Quantidade = 5,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new ItemPedido
            {
                Id = 4,
                IdProduto = 4,
                PedidoId = 2,
                NomeProduto = "Item X",
                PrecoUnitario = 75.00m,
                Quantidade = 3,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ItemPedido
            {
                Id = 5,
                IdProduto = 5,
                PedidoId = 3,
                NomeProduto = "Produto Deletado",
                PrecoUnitario = 150.00m,
                Quantidade = 1,
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        await context.ItensPedido.AddRangeAsync(items);
        await context.SaveChangesAsync();

        return context;
    }

    #region GetAllItemPedidosAsync Tests

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldReturnAllItems_WhenNoFiltersApplied()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync();

        // Assert
        Assert.Equal(4, result.Count); // 4 itens não deletados
        Assert.DoesNotContain(result, i => i.IsDeleted);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldReturnEmptyList_WhenNoItemsExist()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByNomeProduto_WhenNomeProdutoProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(nomeProduto: "Produto");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, i => Assert.Contains("Produto", i.NomeProduto));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByMinPrecoUnitario_WhenMinPrecoUnitarioProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(minPrecoUnitario: 50);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, i => Assert.True(i.PrecoUnitario >= 50));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByMaxPrecoUnitario_WhenMaxPrecoUnitarioProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(maxPrecoUnitario: 75);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, i => Assert.True(i.PrecoUnitario <= 75));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByPrecoUnitarioRange_WhenMinAndMaxPrecoUnitarioProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(minPrecoUnitario: 50, maxPrecoUnitario: 100);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, i => Assert.InRange(i.PrecoUnitario, 50, 100));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByMinQuantidade_WhenMinQuantidadeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(minQuantidade: 3);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.True(i.Quantidade >= 3));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByMaxQuantidade_WhenMaxQuantidadeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(maxQuantidade: 3);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, i => Assert.True(i.Quantidade <= 3));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldFilterByQuantidadeRange_WhenMinAndMaxQuantidadeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(minQuantidade: 2, maxQuantidade: 3);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.InRange(i.Quantidade, 2, 3));
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldSortByNomeProdutoAscending_WhenSortByNomeProdutoAndAscendingTrue()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(sortBy: "nomeproduto", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("Item X", result[0].NomeProduto);
        Assert.Equal("Produto A", result[1].NomeProduto);
        Assert.Equal("Produto B", result[2].NomeProduto);
        Assert.Equal("Produto C", result[3].NomeProduto);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldSortByPrecoUnitarioDescending_WhenSortByPrecoUnitarioAndAscendingFalse()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(sortBy: "precounitario", ascending: false);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(100.00m, result[0].PrecoUnitario);
        Assert.Equal(75.00m, result[1].PrecoUnitario);
        Assert.Equal(50.00m, result[2].PrecoUnitario);
        Assert.Equal(25.00m, result[3].PrecoUnitario);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldSortByQuantidadeAscending_WhenSortByQuantidadeAndAscendingTrue()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(sortBy: "quantidade", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(1, result[0].Quantidade);
        Assert.Equal(2, result[1].Quantidade);
        Assert.Equal(3, result[2].Quantidade);
        Assert.Equal(5, result[3].Quantidade);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldSortByCreatedAtAscending_WhenSortByNotProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync(ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        // Verificar que está ordenado por CreatedAt crescente (mais antigo primeiro)
        Assert.Equal(1, result[0].Id); // Item de 5 dias atrás
        Assert.Equal(2, result[1].Id); // Item de 4 dias atrás
        Assert.Equal(3, result[2].Id); // Item de 3 dias atrás
        Assert.Equal(4, result[3].Id); // Item de 2 dias atrás
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldApplyPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var page1 = await repository.GetAllItemPedidosAsync(page: 1, pageSize: 2);
        var page2 = await repository.GetAllItemPedidosAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task GetAllItemPedidosAsync_ShouldExcludeDeletedItems_Always()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetAllItemPedidosAsync();

        // Assert
        Assert.DoesNotContain(result, i => i.IsDeleted);
        Assert.DoesNotContain(result, i => i.NomeProduto == "Produto Deletado");
    }

    #endregion

    #region GetItemPedidoByIdAsync Tests

    [Fact]
    public async Task GetItemPedidoByIdAsync_ShouldReturnItemPedido_WhenItemPedidoExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetItemPedidoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Produto A", result.NomeProduto);
        Assert.Equal(50.00m, result.PrecoUnitario);
        Assert.Equal(2, result.Quantidade);
    }

    [Fact]
    public async Task GetItemPedidoByIdAsync_ShouldReturnNull_WhenItemPedidoDoesNotExist()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetItemPedidoByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemPedidoByIdAsync_ShouldReturnNull_WhenItemPedidoIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.GetItemPedidoByIdAsync(5); // Item Deletado

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddItemPedidoAsync Tests

    [Fact]
    public async Task AddItemPedidoAsync_ShouldAddItemPedido_Successfully()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ItemPedidoRepository(context);
        var item = new ItemPedido
        {
            IdProduto = 10,
            PedidoId = 1,
            NomeProduto = "Produto Novo",
            PrecoUnitario = 200.00m,
            Quantidade = 4
        };

        // Act
        await repository.AddItemPedidoAsync(item);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.ItensPedido.FindAsync(item.Id);
        Assert.NotNull(result);
        Assert.Equal("Produto Novo", result.NomeProduto);
        Assert.Equal(200.00m, result.PrecoUnitario);
        Assert.Equal(4, result.Quantidade);
    }

    [Fact]
    public async Task AddItemPedidoAsync_ShouldSetCreatedAtAndUpdatedAt_WhenAdding()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new ItemPedidoRepository(context);
        var beforeAdd = DateTime.UtcNow;
        var item = new ItemPedido
        {
            IdProduto = 10,
            PedidoId = 1,
            NomeProduto = "Produto Novo",
            PrecoUnitario = 200.00m,
            Quantidade = 4
        };

        // Act
        await repository.AddItemPedidoAsync(item);
        await repository.SaveChangesAsync();
        var afterAdd = DateTime.UtcNow;

        // Assert
        var result = await context.ItensPedido.FindAsync(item.Id);
        Assert.NotNull(result);
        Assert.InRange(result.CreatedAt, beforeAdd, afterAdd);
        Assert.InRange(result.UpdatedAt, beforeAdd, afterAdd);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    #endregion

    #region UpdateItemPedidoAsync Tests

    [Fact]
    public async Task UpdateItemPedidoAsync_ShouldUpdateItemPedido_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);
        var item = await repository.GetItemPedidoByIdAsync(1);
        item.NomeProduto = "Produto A Atualizado";
        item.PrecoUnitario = 999.99m;
        item.Quantidade = 10;

        // Act
        await repository.UpdateItemPedidoAsync(item);
        await context.SaveChangesAsync();

        // Assert
        var result = await context.ItensPedido.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Produto A Atualizado", result.NomeProduto);
        Assert.Equal(999.99m, result.PrecoUnitario);
        Assert.Equal(10, result.Quantidade);
    }

    [Fact]
    public async Task UpdateItemPedidoAsync_ShouldUpdateUpdatedAt_WhenUpdating()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);
        var item = await repository.GetItemPedidoByIdAsync(1);
        var originalUpdatedAt = item.UpdatedAt;
        
        // Aguarda um tempo para garantir que o UpdatedAt seja diferente
        await Task.Delay(10);
        
        item.Quantidade = 10;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await repository.UpdateItemPedidoAsync(item);
        await context.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var result = await context.ItensPedido.FindAsync(1);
        Assert.NotNull(result);
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
        Assert.NotEqual(originalUpdatedAt, result.UpdatedAt);
    }

    #endregion

    #region DeleteItemPedidoAsync Tests

    [Fact]
    public async Task DeleteItemPedidoAsync_ShouldMarkAsDeleted_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);
        var item = await repository.GetItemPedidoByIdAsync(1);

        // Act
        await repository.DeleteItemPedidoAsync(item);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.ItensPedido.FindAsync(1);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeleteItemPedidoAsync_ShouldSetDeletedAt_WhenDeleting()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new ItemPedidoRepository(context);
        var item = await repository.GetItemPedidoByIdAsync(1);
        var beforeDelete = DateTime.UtcNow;

        // Act
        await repository.DeleteItemPedidoAsync(item);
        await repository.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var result = await context.ItensPedido.FindAsync(1);
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
        var repository = new ItemPedidoRepository(context);
        var item = new ItemPedido
        {
            IdProduto = 10,
            PedidoId = 1,
            NomeProduto = "Produto Novo",
            PrecoUnitario = 200.00m,
            Quantidade = 4
        };
        await repository.AddItemPedidoAsync(item);

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
        var repository = new ItemPedidoRepository(context);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.False(result);
    }

    #endregion
}
