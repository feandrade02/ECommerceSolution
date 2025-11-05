using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;

namespace Sales.API.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly SalesContext _context;

    public PedidoRepository(SalesContext context)
    {
        _context = context;
    }

    public async Task AddPedidoAsync(Pedido pedido)
    {
        var now = DateTime.UtcNow;
        pedido.CreatedAt = now;
        pedido.UpdatedAt = now;
        await _context.Pedidos.AddAsync(pedido);
    }

    public Task DeletePedidoAsync(Pedido pedido)
    {
        pedido.IsDeleted = true;
        var now = DateTime.UtcNow;
        pedido.DeletedAt = now;
        pedido.Status = StatusPedido.Cancelado;
        _context.Pedidos.Update(pedido);

        // Garante que os itens também sejam marcados como deletados (soft delete)
        if (pedido.Itens != null)
        {
            foreach (var item in pedido.Itens)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
            }
        }
        return Task.CompletedTask;
    }

    public async Task<List<Pedido>> GetAllPedidosAsync(
        int page = 1,
        int pageSize = 10,
        string sortBy = null,
        bool ascending = true,
        int? minTotalValue = null,
        int? maxTotalValue = null
    )
    {
        var query = _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Itens)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (minTotalValue.HasValue)
        {
            query = query.Where(p => p.ValorTotal >= minTotalValue.Value);
        }

        if (maxTotalValue.HasValue)
        {
            query = query.Where(p => p.ValorTotal <= maxTotalValue.Value);
        }

        query = (sortBy ?? string.Empty).ToLower() switch
        {
            "valortotal" => ascending ? query.OrderBy(p => p.ValorTotal) : query.OrderByDescending(p => p.ValorTotal),
            _ => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
        };

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<Pedido> GetPedidoByIdAsync(int id)
    {
        return await _context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public Task UpdatePedidoAsync(Pedido pedido)
    {
        pedido.UpdatedAt = DateTime.UtcNow;
        _context.Pedidos.Update(pedido);
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
