namespace Sales.API.Domain.Entities;

public class ItemPedido
{
    public int Id { get; set; }

    public int IdProduto { get; set; }

    public int PedidoId { get; set; }

    public string NomeProduto { get; set; } = "";

    public decimal PrecoUnitario { get; set; }

    public int Quantidade { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
