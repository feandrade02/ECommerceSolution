namespace Sales.API.Domain.Entities;

public class ItemPedido
{
    public int Id { get; set; }
    public int IdProduto { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; }
    public string NomeProduto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
