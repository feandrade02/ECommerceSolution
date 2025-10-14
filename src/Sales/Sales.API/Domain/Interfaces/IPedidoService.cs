using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;

namespace Sales.API.Domain.Interfaces;

public interface IPedidoService
{
    Task<List<Pedido>> GetAllPedidosAsync(GetAllPedidosDTO request);
    Task<Pedido> GetPedidoByIdAsync(int id);
    Task<Pedido> AddPedidoAsync(PedidoDTO pedidoDTO);
}
