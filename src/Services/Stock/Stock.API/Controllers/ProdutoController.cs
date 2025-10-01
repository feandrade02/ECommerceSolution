using Microsoft.AspNetCore.Mvc;
using Stock.API.Domain.DTOs;
using Stock.API.Interfaces;
using Stock.API.ModelViews;
using Stock.Domain.Entities;

namespace Stock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutoController : ControllerBase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly ILogger<ProdutoController> _logger;

        public ProdutoController(IProdutoRepository produtoRepository, ILogger<ProdutoController> logger)
        {
            _produtoRepository = produtoRepository;
            _logger = logger;
        }

        private bool ValidateProdutoDTO(ProdutoDTO produtoDTO, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(produtoDTO.Nome))
            {
                errors.Add("O nome do produto é obrigatório.");
            }

            if (produtoDTO.Preco <= 0)
            {
                errors.Add("O preço do produto deve ser maior que zero.");
            }

            if (produtoDTO.QuantidadeEstoque < 0)
            {
                errors.Add("A quantidade em estoque não pode ser negativa.");
            }

            return errors.Count == 0;
        }

        [HttpGet("ObterTodos")]
        public async Task<IActionResult> GetAllProducts(
            int page = 1,
            int pageSize = 10,
            string name = null,
            bool ascending = true,
            int? minPrice = null,
            int? maxPrice = null,
            int? minStock = null,
            int? maxStock = null
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

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                validationErrors.Messages.Add("O preço mínimo não pode ser maior que o preço máximo.");
            }

            if (minStock.HasValue && maxStock.HasValue && minStock > maxStock)
            {
                validationErrors.Messages.Add("O estoque mínimo não pode ser maior que o estoque máximo.");
            }

            if (validationErrors.Messages.Count > 0)
            {
                return BadRequest(validationErrors);
            }

            try
            {
                var produtos = await _produtoRepository.GetAllProducts(
                    page, pageSize, name, ascending, minPrice, maxPrice, minStock, maxStock
                );

                var produtosModelView = produtos.Select(p => new ProdutoModelView
                {
                    Id = p.Id,
                    Nome = p.Nome,
                    Descricao = p.Descricao,
                    Preco = p.Preco,
                    QuantidadeEstoque = p.QuantidadeEstoque
                }).ToList();

                return Ok(produtosModelView);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar obter os produtos.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
            }
        }

        [HttpGet("ObterPorId/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var produto = await _produtoRepository.GetProductById(id);

                if (produto == null)
                {
                    return NotFound("Produto não encontrado.");
                }

                var produtoModelView = new ProdutoModelView
                {
                    Id = produto.Id,
                    Nome = produto.Nome,
                    Descricao = produto.Descricao,
                    Preco = produto.Preco,
                    QuantidadeEstoque = produto.QuantidadeEstoque
                };
                return Ok(produtoModelView);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar obter o produto.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
            }
        }

        [HttpPost("Cadastrar")]
        public async Task<IActionResult> AddProduct(ProdutoDTO produtoDTO)
        {
            if (!ValidateProdutoDTO(produtoDTO, out var errors))
            {
                return BadRequest(new ValidationErrors { Messages = errors });
            }

            var produto = new Produto
            {
                Nome = produtoDTO.Nome,
                Descricao = produtoDTO.Descricao,
                Preco = produtoDTO.Preco,
                QuantidadeEstoque = produtoDTO.QuantidadeEstoque
            };

            try
            {
                await _produtoRepository.AddProduct(produto);
                await _produtoRepository.SaveChangesAsync();

                var produtoModelView = new ProdutoModelView
                {
                    Id = produto.Id,
                    Nome = produto.Nome,
                    Descricao = produto.Descricao,
                    Preco = produto.Preco,
                    QuantidadeEstoque = produto.QuantidadeEstoque
                };

                return CreatedAtAction(nameof(GetProductById), new { id = produtoModelView.Id }, produtoModelView);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar cadastrar o produto.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
            }
        }

        [HttpPut("Atualizar/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProdutoDTO produtoDTO)
        {
            if (!ValidateProdutoDTO(produtoDTO, out var errors))
            {
                return BadRequest(new ValidationErrors { Messages = errors });
            }

            try
            {
                var produto = await _produtoRepository.GetProductById(id);

                if (produto == null)
                {
                    return NotFound("Produto não encontrado.");
                }

                produto.Nome = produtoDTO.Nome;
                produto.Descricao = produtoDTO.Descricao;
                produto.Preco = produtoDTO.Preco;
                produto.QuantidadeEstoque = produtoDTO.QuantidadeEstoque;

                _produtoRepository.UpdateProduct(produto);
                await _produtoRepository.SaveChangesAsync();

                var produtoModelView = new ProdutoModelView
                {
                    Id = produto.Id,
                    Nome = produto.Nome,
                    Descricao = produto.Descricao,
                    Preco = produto.Preco,
                    QuantidadeEstoque = produto.QuantidadeEstoque
                };
                return Ok(produtoModelView);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar atualizar o produto.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
            }
        }

        [HttpDelete("Deletar/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var produto = await _produtoRepository.GetProductById(id);

                if (produto == null)
                {
                    return NotFound("Produto não encontrado.");
                }

                _produtoRepository.DeleteProduct(produto);
                await _produtoRepository.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao tentar deletar o produto.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
            }
        }
    }
}