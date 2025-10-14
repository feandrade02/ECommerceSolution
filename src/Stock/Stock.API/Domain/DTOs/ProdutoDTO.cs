namespace Stock.API.Domain.DTOs;

public record ProdutoDTO
{
    public string Nome { get; init; }
    public string Descricao { get; init; }
    public decimal Preco { get; init; }
    public int QuantidadeEstoque { get; init; }
}
