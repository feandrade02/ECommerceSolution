using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;

namespace Sales.API.Domain.Interfaces;

public interface IPedidoRepository
{
    Task<List<Pedido>> GetAllPedidosAsync(
        int page = 1,
        int pageSize = 10,
        string sortBy = null,
        bool ascending = true,
        StatusPedido? status = null,
        int? minTotalValue = null,
        int? maxTotalValue = null
    );

    Task<Pedido> GetPedidoByIdAsync(int id);

    Task AddPedidoAsync(Pedido pedido);

    Task UpdatePedidoAsync(Pedido pedido);

    Task DeletePedidoAsync(Pedido pedido);

    Task<bool> SaveChangesAsync();
}
