namespace Sales.API.Domain.DTOs;

public record PedidoDTO
{
    public int IdCliente { get; set; }
    public List<ItemPedidoDTO> Itens { get; set; }
}
