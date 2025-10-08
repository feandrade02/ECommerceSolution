using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;

namespace Sales.API.Domain.Interfaces
{
    public interface IPedidoService
    {
        Task<Pedido> CriarPedidoAsync(PedidoDTO pedidoDTO);
    }
}