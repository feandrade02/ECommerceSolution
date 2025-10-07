using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Entities;
using Sales.API.Interfaces;

namespace Sales.API.Repositories
{
    public class ItemPedidoRepository : IItemPedidoRepository
    {
        private readonly SalesContext _context;

        public ItemPedidoRepository(SalesContext context)
        {
            _context = context;
        }

        public async Task AddItemPedidoAsync(ItemPedido itemPedido)
        {
            var now = DateTime.UtcNow;
            itemPedido.CreatedAt = now;
            itemPedido.UpdatedAt = now;
            await _context.ItensPedido.AddAsync(itemPedido);
        }

        public Task DeleteItemPedidoAsync(ItemPedido itemPedido)
        {
            var now = DateTime.UtcNow;
            itemPedido.IsDeleted = true;
            itemPedido.DeletedAt = now;
            _context.ItensPedido.Update(itemPedido);
            return Task.CompletedTask;
        }

        public async Task<List<ItemPedido>> GetAllItemPedidosAsync(
            int page = 1,
            int pageSize = 10,
            string nomeProduto = null,
            string sortBy = null,
            bool ascending = true,
            int? minPrecoUnitario = null,
            int? maxPrecoUnitario = null,
            int? minQuantidade = null,
            int? maxQuantidade = null
        )
        {
            var query = _context.ItensPedido.AsNoTracking().Where(i => !i.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(nomeProduto))
            {
                query = query.Where(i => i.NomeProduto.Contains(nomeProduto));
            }

            if (minPrecoUnitario.HasValue)
            {
                query = query.Where(i => i.PrecoUnitario >= minPrecoUnitario.Value);
            }

            if (maxPrecoUnitario.HasValue)
            {
                query = query.Where(i => i.PrecoUnitario <= maxPrecoUnitario.Value);
            }

            if (minQuantidade.HasValue)
            {
                query = query.Where(i => i.Quantidade >= minQuantidade.Value);
            }

            if (maxQuantidade.HasValue)
            {
                query = query.Where(i => i.Quantidade <= maxQuantidade.Value);
            }

            query = sortBy.ToLower() switch
            {
                "nomeproduto" => ascending ? query.OrderBy(i => i.NomeProduto) : query.OrderByDescending(i => i.NomeProduto),
                "precounitario" => ascending ? query.OrderBy(i => i.PrecoUnitario) : query.OrderByDescending(i => i.PrecoUnitario),
                "quantidade" => ascending ? query.OrderBy(i => i.Quantidade) : query.OrderByDescending(i => i.Quantidade),
                _ => ascending ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt),
            };

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<ItemPedido> GetItemPedidoByIdAsync(int id)
        {
            return await _context.ItensPedido.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        }

        public Task UpdateItemPedidoAsync(ItemPedido itemPedido)
        {
            itemPedido.UpdatedAt = DateTime.UtcNow;
            _context.ItensPedido.Update(itemPedido);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}