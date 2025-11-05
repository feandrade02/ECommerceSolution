using Sales.API.Domain.Entities;

namespace Sales.API.Domain.Interfaces;

public interface IItemPedidoRepository
{
    Task<List<ItemPedido>> GetAllItemPedidosAsync(
        int page = 1,
        int pageSize = 10,
        string nomeProduto = null,
        string sortBy = null,
        bool ascending = true,
        int? minPrecoUnitario = null,
        int? maxPrecoUnitario = null,
        int? minQuantidade = null,
        int? maxQuantidade = null
    );
    Task<ItemPedido> GetItemPedidoByIdAsync(int id);
    Task AddItemPedidoAsync(ItemPedido itemPedido);
    Task UpdateItemPedidoAsync(ItemPedido itemPedido);
    Task DeleteItemPedidoAsync(ItemPedido itemPedido);
    Task<bool> SaveChangesAsync();
}
