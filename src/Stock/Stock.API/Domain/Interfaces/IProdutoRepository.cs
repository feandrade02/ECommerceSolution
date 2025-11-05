using Stock.API.Domain.Entities;

namespace Stock.API.Domain.Interfaces;

public interface IProdutoRepository
{
    Task<List<Produto>> GetAllProdutosAsync(
        int page = 1,
        int pageSize = 10,
        string name = null,
        string sortBy = null,
        bool ascending = true,
        int? minPrice = null,
        int? maxPrice = null,
        int? minStock = null,
        int? maxStock = null
    );
    Task<Produto> GetProdutoByIdAsync(int id);
    Task AddProdutoAsync(Produto produto);
    Task UpdateProdutoAsync(Produto produto);
    Task DeleteProdutoAsync(Produto produto);
    Task<bool> SaveChangesAsync();
}
