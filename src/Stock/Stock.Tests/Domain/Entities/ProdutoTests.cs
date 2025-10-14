using Stock.Domain.Entities;

namespace Stock.Tests.Domain.Entities;

public class ProdutoTests
{
    [Fact]
    public void Produto_SetProperties_ShouldHoldCorrectValues()
    {
        // Arrange
        var produto = new Produto();
        var now = DateTime.UtcNow;

        // Act
        produto.Id = 1;
        produto.Nome = "Produto Teste";
        produto.Descricao = "Descrição do Produto Teste";
        produto.Preco = 99.99m;
        produto.QuantidadeEstoque = 100;
        produto.IsDeleted = false;
        produto.CreatedAt = now;
        produto.UpdatedAt = now;
        produto.DeletedAt = null;

        // Assert
        Assert.Equal(1, produto.Id);
        Assert.Equal("Produto Teste", produto.Nome);
        Assert.Equal("Descrição do Produto Teste", produto.Descricao);
        Assert.Equal(99.99m, produto.Preco);
        Assert.Equal(100, produto.QuantidadeEstoque);
        Assert.False(produto.IsDeleted);
        Assert.Equal(now, produto.CreatedAt);
        Assert.Equal(now, produto.UpdatedAt);
        Assert.Null(produto.DeletedAt);
    }

    [Fact]
    public void Produto_WhenSoftDeleted_ShouldUpdateFlagsCorrectly()
    {
        // Arrange
        var produto = new Produto { Nome = "Produto a ser deletado" };
        var deletionDate = DateTime.UtcNow;

        // Act
        produto.IsDeleted = true;
        produto.DeletedAt = deletionDate;

        // Assert
        Assert.True(produto.IsDeleted);
        Assert.NotNull(produto.DeletedAt);
        Assert.Equal(deletionDate, produto.DeletedAt);
    }
}
