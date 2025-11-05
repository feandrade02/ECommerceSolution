namespace Sales.API.Domain.ModelViews;

public class ItemPedidoModelView
{
    public int IdProduto { get; set; }
    public string NomeProduto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
}
