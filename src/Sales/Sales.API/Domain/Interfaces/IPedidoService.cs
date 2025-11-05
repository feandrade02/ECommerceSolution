using Sales.API.Domain.Entities;
using Sales.API.Domain.DTOs;

namespace Sales.API.Domain.Interfaces;

public interface IPedidoService
{
    Task<List<Pedido>> GetAllPedidosAsync(
        int page,
        int pageSize,
        string sortBy,
        bool ascending,
        int? minTotalValue,
        int? maxTotalValue
    );
    Task<Pedido> GetPedidoByIdAsync(int id);
    Task<Pedido> CreatePedidoFromDTOAsync(PedidoDTO pedidoDTO);
    Task<bool> AddPedidoAsync(Pedido pedido);
    Task<bool> UpdatePedidoAsync(Pedido pedido);
    Task<bool> DeletePedidoAsync(Pedido pedido);
}
