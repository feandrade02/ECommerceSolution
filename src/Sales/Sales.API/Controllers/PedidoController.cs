using Microsoft.AspNetCore.Mvc;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.ModelViews;

namespace Sales.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPedidoService _pedidoService;
        private readonly ILogger<PedidoController> _logger;

        public PedidoController(IPedidoRepository pedidoRepository, IPedidoService pedidoService, ILogger<PedidoController> logger)
        {
            _pedidoRepository = pedidoRepository;
            _pedidoService = pedidoService;
            _logger = logger;
        }

        [HttpGet("ObterTodos")]
        public async Task<IActionResult> GetAllPedidos(
            int page = 1,
            int pageSize = 10,
            string sortBy = null,
            bool ascending = true,
            StatusPedido? status = null,
            int? minTotalValue = null,
            int? maxTotalValue = null
        )
        {
            var validationErrors = new ValidationErrors { Messages = new List<string>() };

            if (page <= 0)
            {
                validationErrors.Messages.Add("O número da página deve ser maior que zero.");
            }

            if (pageSize <= 0)
            {
                validationErrors.Messages.Add("O tamanho da página deve ser maior que zero.");
            }

            if (sortBy != null && sortBy.ToLower() != "valortotal")
            {
                validationErrors.Messages.Add("O campo de ordenação deve ser 'valortotal' ou vazio.");
            }

            if (minTotalValue.HasValue && minTotalValue < 0)
            {
                validationErrors.Messages.Add("O valor mínimo do pedido não pode ser negativo.");
            }

            if (maxTotalValue.HasValue && maxTotalValue < 0)
            {
                validationErrors.Messages.Add("O valor máximo do pedido não pode ser negativo.");
            }

            if (minTotalValue.HasValue && maxTotalValue.HasValue && minTotalValue > maxTotalValue)
            {
                validationErrors.Messages.Add("O valor mínimo do pedido não pode ser maior que o valor máximo.");
            }

            if (validationErrors.Messages.Count > 0)
            {
                return BadRequest(validationErrors);
            }
            try
            {
                var pedidos = await _pedidoRepository.GetAllPedidosAsync(page, pageSize, sortBy, ascending, status, minTotalValue, maxTotalValue);

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
                var pedido = await _pedidoRepository.GetPedidoByIdAsync(id);

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
                var pedido = await _pedidoService.CriarPedidoAsync(pedidoDTO);

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
}