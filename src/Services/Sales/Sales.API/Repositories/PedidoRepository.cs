using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Interfaces;

namespace Sales.API.Repositories
{
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
            var now = DateTime.UtcNow;
            pedido.IsDeleted = true;
            pedido.DeletedAt = now;
            _context.Pedidos.Update(pedido);
            return Task.CompletedTask;
        }

        public async Task<List<Pedido>> GetAllPedidosAsync(
            int page = 1,
            int pageSize = 10,
            string sortBy = null,
            bool ascending = true,
            StatusPedido? status = null,
            int? minTotalValue = null,
            int? maxTotalValue = null
        )
        {
            var query = _context.Pedidos.AsNoTracking().Where(p => !p.IsDeleted).AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (minTotalValue.HasValue)
            {
                query = query.Where(p => p.ValorTotal >= minTotalValue.Value);
            }

            if (maxTotalValue.HasValue)
            {
                query = query.Where(p => p.ValorTotal <= maxTotalValue.Value);
            }

            if (sortBy.ToLower() == "valortotal")
            {
                query = ascending ? query.OrderBy(p => p.ValorTotal) : query.OrderByDescending(p => p.ValorTotal);
            }
            else // Default sorting by CreatedAt
            {
                query = ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
            }

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<Pedido> GetPedidoByIdAsync(int id)
        {
            return await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
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
}