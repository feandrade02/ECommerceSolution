using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.ModelViews;

namespace Sales.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sales")]
public class PedidoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;
    private readonly ILogger<PedidoController> _logger;

    public PedidoController(IPedidoService pedidoService, ILogger<PedidoController> logger)
    {
        _pedidoService = pedidoService;
        _logger = logger;
    }

    private static bool ValidadePedidoDTO(PedidoDTO pedidoDTO, out List<string> errors)
    {
        errors = [];

        if (pedidoDTO == null)
        {
            errors.Add("O pedido não pode ser vazio ou nulo.");
        }

        if (pedidoDTO.IdCliente <= 0)
        {
            errors.Add("O ID do cliente é obrigatório e deve ser maior que zero.");
        }

        if (pedidoDTO.Itens == null || pedidoDTO.Itens.Count == 0)
        {
            errors.Add("O pedido deve conter pelo menos um item.");
        }

        if (pedidoDTO.Itens != null)
        {
            foreach (var item in pedidoDTO.Itens)
            {
                if (item.IdProduto <= 0)
                {
                    errors.Add("O ID do produto é obrigatório e deve ser maior que zero.");
                }
                if (item.Quantidade <= 0)
                {
                    errors.Add("A quantidade do produto deve ser maior que zero.");
                }
            }
        }

        return errors.Count == 0;
    }

    private static PedidoModelView MapToPedidoModelView(Pedido pedido)
    {
        return new PedidoModelView
        {
            Id = pedido.Id,
            IdCliente = pedido.IdCliente,
            ValorTotal = pedido.ValorTotal,
            Status = pedido.Status,
            Itens = [.. pedido.Itens.Select(item => new ItemPedidoModelView
            {
                IdProduto = item.IdProduto,
                NomeProduto = item.NomeProduto,
                PrecoUnitario = item.PrecoUnitario,
                Quantidade = item.Quantidade
            })]
        };
    }

    [HttpGet("ObterTodos")]
    public async Task<IActionResult> GetAllPedidos(
        int page = 1,
        int pageSize = 10,
        string sortBy = null,
        bool ascending = true,
        int? minTotalValue = null,
        int? maxTotalValue = null
    )
    {
        var validationErrors = new ValidationErrors { Messages = [] };

        if (page <= 0)
        {
            validationErrors.Messages.Add("O número da página deve ser maior que zero.");
        }
        if (pageSize <= 0)
        {
            validationErrors.Messages.Add("O tamanho da página deve ser maior que zero.");
        }
        if (sortBy != null && !string.Equals(sortBy, "valortotal"))
        {
            validationErrors.Messages.Add($"O campo de ordenação deve ser 'valortotal' ou vazio.");
        }
        if (minTotalValue.HasValue && minTotalValue < 0)
        {
            validationErrors.Messages.Add("O valor total mínimo deve ser maior ou igual a zero.");
        }
        if (maxTotalValue.HasValue && maxTotalValue < 0)
        {
            validationErrors.Messages.Add("O valor total máximo deve ser maior ou igual a zero.");
        }
        if (minTotalValue.HasValue && maxTotalValue.HasValue && minTotalValue > maxTotalValue)
        {
            validationErrors.Messages.Add("O valor total mínimo não pode ser maior que o valor total máximo.");
        }
        if (validationErrors.Messages.Count > 0)
        {
            return BadRequest(validationErrors);
        }
        
        try
        {
            var pedidos = await _pedidoService.GetAllPedidosAsync(
                page, pageSize, sortBy, ascending, minTotalValue, maxTotalValue
            );

            var pedidosModelView = pedidos.Select(MapToPedidoModelView).ToList();

            return Ok(pedidosModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedidos.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpGet("ObterPorId/{id}")]
    public async Task<IActionResult> GetPedidoById(int id)
    {
        try
        {
            var pedido = await _pedidoService.GetPedidoByIdAsync(id);

            if (pedido == null)
            {
                return NotFound("Pedido não encontrado.");
            }

            var pedidoModelView = MapToPedidoModelView(pedido);

            return Ok(pedidoModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedido.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpPost("Cadastrar")]
    public async Task<IActionResult> AddPedido(PedidoDTO pedidoDTO)
    {
        if (!ValidadePedidoDTO(pedidoDTO, out var errors))
        {
            return BadRequest(new ValidationErrors { Messages = errors });
        }

        try
        {
            // Orquestra a criação da entidade Pedido a partir do DTO
            var pedido = await _pedidoService.CreatePedidoFromDTOAsync(pedidoDTO);

            var createdPedido = await _pedidoService.AddPedidoAsync(pedido);

            if (!createdPedido)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Não foi possível salvar o pedido. O serviço de persistência falhou.");
            }

            var pedidoModelView = MapToPedidoModelView(pedido);
            return CreatedAtAction(nameof(GetPedidoById), new { id = pedido.Id }, pedidoModelView);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Ex: Estoque insuficiente
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar pedido.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpDelete("Excluir/{id}")]
    public async Task<IActionResult> DeletePedido(int id)
    {
        try
        {
            var pedido = await _pedidoService.GetPedidoByIdAsync(id);

            if (pedido == null)
            {
                return NotFound("Pedido não encontrado.");
            }

            var deletedPedido = await _pedidoService.DeletePedidoAsync(pedido);

            if (!deletedPedido)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Não foi possível deletar o pedido.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar pedido.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }
}