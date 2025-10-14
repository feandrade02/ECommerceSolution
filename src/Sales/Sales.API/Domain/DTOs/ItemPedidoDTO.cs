namespace Sales.API.Domain.DTOs;

public record ItemPedidoDTO
{
    public int IdProduto { get; set; }
    public int Quantidade { get; set; }
}
