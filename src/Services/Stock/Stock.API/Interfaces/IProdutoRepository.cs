using Stock.Domain.Entities;

namespace Stock.API.Interfaces
{
    public interface IProdutoRepository
    {
        Task<List<Produto>> GetAllProducts(
            int page = 1,
            int pageSize = 10,
            string name = null,
            bool ascending = true,
            int? minPrice = null,
            int? maxPrice = null,
            int? minStock = null,
            int? maxStock = null
        );
        Task<Produto> GetProductById(int id);
        Task AddProduct(Produto product);
        void UpdateProduct(Produto product);
        void DeleteProduct(Produto product);
        Task<bool> SaveChangesAsync();
    }
}