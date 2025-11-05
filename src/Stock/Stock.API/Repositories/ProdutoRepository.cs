using Microsoft.EntityFrameworkCore;
using Stock.API.Domain.Interfaces;
using Stock.API.Context;
using Stock.API.Domain.Entities;

namespace Stock.API.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly StockContext _context;

    public ProdutoRepository(StockContext context)
    {
        _context = context;
    }

    public async Task AddProdutoAsync(Produto produto)
    {
        var now = DateTime.UtcNow;
        produto.CreatedAt = now;
        produto.UpdatedAt = now;
        await _context.Produtos.AddAsync(produto);
    }

    public Task DeleteProdutoAsync(Produto produto)
    {
        produto.IsDeleted = true;
        produto.DeletedAt = DateTime.UtcNow;
        _context.Produtos.Update(produto);
        return Task.CompletedTask;
    }

    public async Task<List<Produto>> GetAllProdutosAsync(
        int page = 1,
        int pageSize = 10,
        string name = null,
        string sortBy = null,
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

        query = (sortBy ?? string.Empty).ToLower() switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "preco" => ascending ? query.OrderBy(p => p.Preco) : query.OrderByDescending(p => p.Preco),
            "quantidadeestoque" => ascending ? query.OrderBy(p => p.QuantidadeEstoque) : query.OrderByDescending(p => p.QuantidadeEstoque),
            _ => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
        };
        
        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<Produto> GetProdutoByIdAsync(int id)
    {
        return await _context.Produtos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public Task UpdateProdutoAsync(Produto produto)
    {
        produto.UpdatedAt = DateTime.UtcNow;
        _context.Produtos.Update(produto);
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
