using Stock.API.Domain.Interfaces;
using Stock.API.Domain.Entities;

namespace Stock.API.Domain.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _produtoRepository;

    public ProdutoService(IProdutoRepository produtoRepository)
    {
        _produtoRepository = produtoRepository;
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
        return await _produtoRepository.GetAllProdutosAsync(
            page, pageSize, name, sortBy, ascending, minPrice, maxPrice, minStock, maxStock
        );
    }

    public async Task<Produto> GetProdutoByIdAsync(int id)
    {
        return await _produtoRepository.GetProdutoByIdAsync(id);
    }

    public async Task<bool> AddProdutoAsync(Produto produto)
    {
        await _produtoRepository.AddProdutoAsync(produto);
        return await _produtoRepository.SaveChangesAsync();
    }

    public Task<bool> UpdateProdutoAsync(Produto produto)
    {
        _produtoRepository.UpdateProdutoAsync(produto);
        return _produtoRepository.SaveChangesAsync();
    }

    public Task<bool> DeleteProdutoAsync(Produto produto)
    {
        _produtoRepository.DeleteProdutoAsync(produto);
        return _produtoRepository.SaveChangesAsync();
    }

    public async Task<bool> UpdateStockAsync(int IdProduto, int quantidade)
    {
        var produto = await _produtoRepository.GetProdutoByIdAsync(IdProduto);
        if (produto == null)
        {
            return false;
        }

        produto.QuantidadeEstoque += quantidade;
        await _produtoRepository.UpdateProdutoAsync(produto);
        return await _produtoRepository.SaveChangesAsync();
    }
}