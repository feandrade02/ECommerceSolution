namespace Contracts;

public record PedidoCriadoEvent
{
    public Guid CorrelationId { get; init; }    
    public List<ItemPedidoReferenceDTO> Itens { get; init; }
}

public record ItemPedidoReferenceDTO
{
    public int IdProduto { get; init; }
    public int Quantidade { get; init; }
}
