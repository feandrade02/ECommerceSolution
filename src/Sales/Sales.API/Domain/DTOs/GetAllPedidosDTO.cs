using Sales.API.Domain.Enums;

namespace Sales.API.Domain.DTOs;

public record GetAllPedidosDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; }
    public bool Ascending { get; set; } = true;
    public StatusPedido? Status { get; set; }
    public int? MinTotalValue { get; set; }
    public int? MaxTotalValue { get; set; }
}
