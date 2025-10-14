using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;

namespace Sales.API.Domain.ModelViews;

public record PedidoModelView
{
    public int Id { get; set; }

    public int IdCliente { get; set; }

    public decimal ValorTotal { get; set; }

    public StatusPedido Status { get; set; }

    public List<ItemPedido> Itens { get; set; } = [];
}
