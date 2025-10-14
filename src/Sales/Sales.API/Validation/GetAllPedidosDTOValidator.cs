using FluentValidation;
using Sales.API.Domain.DTOs;

namespace Sales.API.Validation;

public class GetAllPedidosDTOValidator : AbstractValidator<GetAllPedidosDTO>
{
    public GetAllPedidosDTOValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("A página deve ser maior que zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("O tamanho da página deve ser maior que zero.");

        RuleFor(x => x.MinTotalValue)
            .GreaterThanOrEqualTo(0).When(x => x.MinTotalValue.HasValue)
            .WithMessage("O valor mínimo do pedido deve ser maior ou igual a zero.");

        RuleFor(x => x.MaxTotalValue)
            .GreaterThanOrEqualTo(0).When(x => x.MaxTotalValue.HasValue)
            .WithMessage("O valor máximo do pedido deve ser maior ou igual a zero.");

        RuleFor(x => x)
            .Must(x => !x.MinTotalValue.HasValue || !x.MaxTotalValue.HasValue || x.MinTotalValue <= x.MaxTotalValue)
            .WithMessage("O valor mínimo do pedido não pode ser maior que o valor máximo.");

        RuleFor(x => x.SortBy)
            .Must(BeAValidSortField)
            .WithMessage("O campo de ordenação deve ser 'valorTotal' ou vazio.");
    }

    private bool BeAValidSortField(string sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return true;
        return sortBy.ToLower() == "valortotal";
    }
}
