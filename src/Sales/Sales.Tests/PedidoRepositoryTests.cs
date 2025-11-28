using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Repositories;
using Xunit;

namespace Sales.Tests;

public class PedidoRepositoryTests
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

        var pedidos = new List<Pedido>
        {
            new Pedido 
            { 
                Id = 1, 
                IdCliente = 1, 
                ValorTotal = 100.00m, 
                Status = StatusPedido.Confirmado,
                IsDeleted = false, 
                CreatedAt = DateTime.UtcNow.AddDays(-5), 
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Id = 1, IdProduto = 1, NomeProduto = "Produto A", PrecoUnitario = 50.00m, Quantidade = 2, IsDeleted = false }
                }
            },
            new Pedido 
            { 
                Id = 2, 
                IdCliente = 2, 
                ValorTotal = 250.00m, 
                Status = StatusPedido.Confirmado,
                IsDeleted = false, 
                CreatedAt = DateTime.UtcNow.AddDays(-4), 
                UpdatedAt = DateTime.UtcNow.AddDays(-4),
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Id = 2, IdProduto = 2, NomeProduto = "Produto B", PrecoUnitario = 125.00m, Quantidade = 2, IsDeleted = false }
                }
            },
            new Pedido 
            { 
                Id = 3, 
                IdCliente = 1, 
                ValorTotal = 500.00m, 
                Status = StatusPedido.Confirmado,
                IsDeleted = false, 
                CreatedAt = DateTime.UtcNow.AddDays(-3), 
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Id = 3, IdProduto = 3, NomeProduto = "Produto C", PrecoUnitario = 250.00m, Quantidade = 2, IsDeleted = false }
                }
            },
            new Pedido 
            { 
                Id = 4, 
                IdCliente = 3, 
                ValorTotal = 75.00m, 
                Status = StatusPedido.Confirmado,
                IsDeleted = false, 
                CreatedAt = DateTime.UtcNow.AddDays(-2), 
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Id = 4, IdProduto = 4, NomeProduto = "Produto D", PrecoUnitario = 25.00m, Quantidade = 3, IsDeleted = false }
                }
            },
            new Pedido 
            { 
                Id = 5, 
                IdCliente = 4, 
                ValorTotal = 150.00m, 
                Status = StatusPedido.Cancelado,
                IsDeleted = true, 
                CreatedAt = DateTime.UtcNow.AddDays(-1), 
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                DeletedAt = DateTime.UtcNow.AddDays(-1),
                Itens = new List<ItemPedido>
                {
                    new ItemPedido { Id = 5, IdProduto = 5, NomeProduto = "Produto E", PrecoUnitario = 75.00m, Quantidade = 2, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-1) }
                }
            }
        };

        await context.Pedidos.AddRangeAsync(pedidos);
        await context.SaveChangesAsync();

        return context;
    }

    #region GetAllPedidosAsync Tests

    [Fact]
    public async Task GetAllPedidosAsync_ShouldReturnAllPedidos_WhenNoFiltersApplied()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync();

        // Assert
        Assert.Equal(4, result.Count); // 4 pedidos não deletados
        Assert.DoesNotContain(result, p => p.IsDeleted);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldReturnEmptyList_WhenNoPedidosExist()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldFilterByMinTotalValue_WhenMinTotalValueProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(minTotalValue: 200);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.ValorTotal >= 200));
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldFilterByMaxTotalValue_WhenMaxTotalValueProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(maxTotalValue: 200);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.ValorTotal <= 200));
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldFilterByTotalValueRange_WhenMinAndMaxTotalValueProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(minTotalValue: 100, maxTotalValue: 300);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.InRange(p.ValorTotal, 100, 300));
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldSortByValorTotalAscending_WhenSortByValorTotalAndAscendingTrue()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(sortBy: "valortotal", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(75.00m, result[0].ValorTotal);
        Assert.Equal(100.00m, result[1].ValorTotal);
        Assert.Equal(250.00m, result[2].ValorTotal);
        Assert.Equal(500.00m, result[3].ValorTotal);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldSortByValorTotalDescending_WhenSortByValorTotalAndAscendingFalse()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(sortBy: "valortotal", ascending: false);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal(500.00m, result[0].ValorTotal);
        Assert.Equal(250.00m, result[1].ValorTotal);
        Assert.Equal(100.00m, result[2].ValorTotal);
        Assert.Equal(75.00m, result[3].ValorTotal);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldSortByCreatedAtAscending_WhenSortByNotProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync(ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        // Verificar que está ordenado por CreatedAt crescente (mais antigo primeiro)
        Assert.Equal(1, result[0].Id); // Pedido de 5 dias atrás
        Assert.Equal(2, result[1].Id); // Pedido de 4 dias atrás
        Assert.Equal(3, result[2].Id); // Pedido de 3 dias atrás
        Assert.Equal(4, result[3].Id); // Pedido de 2 dias atrás
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldApplyPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var page1 = await repository.GetAllPedidosAsync(page: 1, pageSize: 2);
        var page2 = await repository.GetAllPedidosAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldExcludeDeletedPedidos_Always()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync();

        // Assert
        Assert.DoesNotContain(result, p => p.IsDeleted);
        Assert.DoesNotContain(result, p => p.Status == StatusPedido.Cancelado);
    }

    [Fact]
    public async Task GetAllPedidosAsync_ShouldIncludeItens_Always()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetAllPedidosAsync();

        // Assert
        Assert.All(result, p => Assert.NotNull(p.Itens));
        Assert.All(result, p => Assert.NotEmpty(p.Itens));
    }

    #endregion

    #region GetPedidoByIdAsync Tests

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldReturnPedido_WhenPedidoExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetPedidoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.IdCliente);
        Assert.Equal(100.00m, result.ValorTotal);
    }

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldReturnNull_WhenPedidoDoesNotExist()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetPedidoByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldReturnNull_WhenPedidoIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetPedidoByIdAsync(5); // Pedido Deletado

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPedidoByIdAsync_ShouldIncludeItens_WhenPedidoExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.GetPedidoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Itens);
        Assert.NotEmpty(result.Itens);
        Assert.Equal("Produto A", result.Itens[0].NomeProduto);
    }

    #endregion

    #region AddPedidoAsync Tests

    [Fact]
    public async Task AddPedidoAsync_ShouldAddPedido_Successfully()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PedidoRepository(context);
        var pedido = new Pedido
        {
            IdCliente = 1,
            ValorTotal = 300.00m,
            Status = StatusPedido.Confirmado,
            Itens = new List<ItemPedido>
            {
                new ItemPedido { IdProduto = 1, NomeProduto = "Produto X", PrecoUnitario = 100.00m, Quantidade = 3 }
            }
        };

        // Act
        await repository.AddPedidoAsync(pedido);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Pedidos.FindAsync(pedido.Id);
        Assert.NotNull(result);
        Assert.Equal(1, result.IdCliente);
        Assert.Equal(300.00m, result.ValorTotal);
    }

    [Fact]
    public async Task AddPedidoAsync_ShouldSetCreatedAtAndUpdatedAt_WhenAdding()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PedidoRepository(context);
        var beforeAdd = DateTime.UtcNow;
        var pedido = new Pedido
        {
            IdCliente = 1,
            ValorTotal = 300.00m,
            Status = StatusPedido.Confirmado
        };

        // Act
        await repository.AddPedidoAsync(pedido);
        await repository.SaveChangesAsync();
        var afterAdd = DateTime.UtcNow;

        // Assert
        var result = await context.Pedidos.FindAsync(pedido.Id);
        Assert.NotNull(result);
        Assert.InRange(result.CreatedAt, beforeAdd, afterAdd);
        Assert.InRange(result.UpdatedAt, beforeAdd, afterAdd);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    #endregion

    #region UpdatePedidoAsync Tests

    [Fact]
    public async Task UpdatePedidoAsync_ShouldUpdatePedido_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);
        pedido.ValorTotal = 999.99m;
        pedido.Status = StatusPedido.Cancelado;

        // Act
        await repository.UpdatePedidoAsync(pedido);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Pedidos.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal(999.99m, result.ValorTotal);
        Assert.Equal(StatusPedido.Cancelado, result.Status);
    }

    [Fact]
    public async Task UpdatePedidoAsync_ShouldUpdateUpdatedAt_WhenUpdating()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);
        var originalUpdatedAt = pedido.UpdatedAt;
        
        // Aguarda um tempo para garantir que o UpdatedAt seja diferente
        await Task.Delay(10);
        
        pedido.ValorTotal = 999.99m;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await repository.UpdatePedidoAsync(pedido);
        await repository.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var result = await context.Pedidos.FindAsync(1);
        Assert.NotNull(result);
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
        Assert.NotEqual(originalUpdatedAt, result.UpdatedAt);
    }

    #endregion

    #region DeletePedidoAsync Tests

    [Fact]
    public async Task DeletePedidoAsync_ShouldMarkAsDeleted_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);

        // Act
        await repository.DeletePedidoAsync(pedido);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Pedidos.FindAsync(1);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeletePedidoAsync_ShouldSetDeletedAt_WhenDeleting()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);
        var beforeDelete = DateTime.UtcNow;

        // Act
        await repository.DeletePedidoAsync(pedido);
        await repository.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var result = await context.Pedidos.FindAsync(1);
        Assert.NotNull(result);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete, afterDelete);
    }

    [Fact]
    public async Task DeletePedidoAsync_ShouldSetStatusToCancelado_WhenDeleting()
    {
        // Arrange
        await using var context = await CreateContextWithData(); 
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);

        // Act
        await repository.DeletePedidoAsync(pedido);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Pedidos.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal(StatusPedido.Cancelado, result.Status);
    }

    [Fact]
    public async Task DeletePedidoAsync_ShouldMarkItensAsDeleted_WhenDeletingPedido()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new PedidoRepository(context);
        var pedido = await repository.GetPedidoByIdAsync(1);
        var beforeDelete = DateTime.UtcNow;

        // Act
        await repository.DeletePedidoAsync(pedido);
        await repository.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var result = await context.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == 1);
        Assert.NotNull(result);
        Assert.NotNull(result.Itens);
        Assert.All(result.Itens, item => Assert.True(item.IsDeleted));
        Assert.All(result.Itens, item => Assert.NotNull(item.DeletedAt));
        Assert.All(result.Itens, item => Assert.InRange(item.DeletedAt.Value, beforeDelete, afterDelete));
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenChangesAreSaved()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PedidoRepository(context);
        var pedido = new Pedido
        {
            IdCliente = 1,
            ValorTotal = 300.00m,
            Status = StatusPedido.Confirmado
        };
        await repository.AddPedidoAsync(pedido);

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
        var repository = new PedidoRepository(context);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.False(result);
    }

    #endregion
}
