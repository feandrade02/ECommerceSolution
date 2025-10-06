using Microsoft.EntityFrameworkCore;
using Stock.API.Interfaces;
using Stock.Context;
using Stock.Domain.Entities;

namespace Stock.API.Repositories
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly StockContext _context;

        public ProdutoRepository(StockContext context)
        {
            _context = context;
        }

        public async Task AddProduct(Produto product)
        {
            var now = DateTime.UtcNow;
            product.CreatedAt = now;
            product.UpdatedAt = now;
            await _context.Produtos.AddAsync(product);
        }

        public void DeleteProduct(Produto product)
        {
            var now = DateTime.UtcNow;
            product.IsDeleted = true;
            product.DeletedAt = now;
            UpdateProduct(product);
        }

        public async Task<List<Produto>> GetAllProducts(
            int page = 1,
            int pageSize = 10,
            string name = null,
            bool ascending = true,
            int? minPrice = null,
            int? maxPrice = null,
            int? minStock = null,
            int? maxStock = null
        )
        {
            var query = _context.Produtos.AsNoTracking().Where(p => !p.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Nome.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Preco >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Preco <= maxPrice.Value);
            }

            if (minStock.HasValue)
            {
                query = query.Where(p => p.QuantidadeEstoque >= minStock.Value);
            }

            if (maxStock.HasValue)
            {
                query = query.Where(p => p.QuantidadeEstoque <= maxStock.Value);
            }

            query = ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome);

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<Produto> GetProductById(int id)
        {
            return await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public void UpdateProduct(Produto product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Produtos.Update(product);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}