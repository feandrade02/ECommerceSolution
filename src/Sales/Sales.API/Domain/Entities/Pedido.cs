using Sales.API.Domain.Enums;

namespace Sales.API.Domain.Entities;

public class Pedido
{
    public int Id { get; set; }
    public int IdCliente { get; set; }
    public decimal ValorTotal { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Confirmado;
    public List<ItemPedido> Itens { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
