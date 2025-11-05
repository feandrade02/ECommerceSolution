namespace Contracts;

public record UpdateStockEvent
{
    public Guid CorrelationId { get; init; }
    public required List<ItemPedidoReference> Itens { get; init; }
}

public record ItemPedidoReference
{
    public int IdProduto { get; init; }
    public int Quantidade { get; init; }
}
