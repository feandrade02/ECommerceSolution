using Microsoft.AspNetCore.Mvc;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.ModelViews;

namespace Sales.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;
    private readonly ILogger<PedidoController> _logger;

    public PedidoController(IPedidoService pedidoService, ILogger<PedidoController> logger)
    {
        _pedidoService = pedidoService;
        _logger = logger;
    }

    [HttpGet("ObterTodos")]
    public async Task<IActionResult> GetAllPedidos([FromQuery] GetAllPedidosDTO request)
    {
        try
        {
            var pedidos = await _pedidoService.GetAllPedidosAsync(request);

            var pedidosModelView = pedidos.Select(p => new PedidoModelView
            {
                Id = p.Id,
                IdCliente = p.IdCliente,
                ValorTotal = p.ValorTotal,
                Status = p.Status,
                Itens = p.Itens
            }).ToList();

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

            var pedidoModelView = new PedidoModelView
            {
                Id = pedido.Id,
                IdCliente = pedido.IdCliente,
                ValorTotal = pedido.ValorTotal,
                Status = pedido.Status,
                Itens = pedido.Itens
            };

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
        try
        {
            var pedido = await _pedidoService.AddPedidoAsync(pedidoDTO);

            var pedidoModelView = new PedidoModelView
            {
                Id = pedido.Id,
                IdCliente = pedido.IdCliente,
                ValorTotal = pedido.ValorTotal,
                Status = pedido.Status,
                Itens = pedido.Itens
            };

            return CreatedAtAction(nameof(GetPedidoById), new { id = pedidoModelView.Id }, pedidoModelView);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
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
}