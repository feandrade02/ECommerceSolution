using Microsoft.AspNetCore.Mvc;
using Stock.API.Domain.DTOs;
using Stock.API.Domain.Interfaces;
using Stock.API.Domain.ModelViews;
using Stock.API.Domain.Entities;

namespace Stock.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoService _produtoService;
    private readonly ILogger<ProdutoController> _logger;

    public ProdutoController(IProdutoService produtoService, ILogger<ProdutoController> logger)
    {
        _produtoService = produtoService;
        _logger = logger;
    }

    private static bool ValidateProdutoDTO(ProdutoDTO produtoDTO, out List<string> errors)
    {
        errors = [];

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
    public async Task<IActionResult> GetAllProdutos(
        int page = 1,
        int pageSize = 10,
        string name = null,
        string sortBy = null,
        bool ascending = true,
        int? minPrice = null,
        int? maxPrice = null,
        int? minStock = null,
        int? maxStock = null
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

        if (minPrice.HasValue && minPrice < 0)
        {
            validationErrors.Messages.Add("O preço mínimo não pode ser negativo.");
        }

        if (maxPrice.HasValue && maxPrice < 0)
        {
            validationErrors.Messages.Add("O preço máximo não pode ser negativo.");
        }

        if (minStock.HasValue && minStock < 0)
        {
            validationErrors.Messages.Add("O estoque mínimo não pode ser negativo.");
        }

        if (maxStock.HasValue && maxStock < 0)
        {
            validationErrors.Messages.Add("O estoque máximo não pode ser negativo.");
        }

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            validationErrors.Messages.Add("O preço mínimo não pode ser maior que o preço máximo.");
        }

        if (minStock.HasValue && maxStock.HasValue && minStock > maxStock)
        {
            validationErrors.Messages.Add("O estoque mínimo não pode ser maior que o estoque máximo.");
        }

        if (sortBy != null && !string.Equals(sortBy, "nome") && !string.Equals(sortBy, "preco") && !string.Equals(sortBy, "quantidadeestoque"))
        {
            validationErrors.Messages.Add("O campo de ordenação deve ser 'nome', 'preco', 'quantidadeestoque' ou vazio.");
        }

        if (validationErrors.Messages.Count > 0)
        {
            return BadRequest(validationErrors);
        }

        try
        {
            var produtos = await _produtoService.GetAllProdutosAsync(
                page, pageSize, name, sortBy, ascending, minPrice, maxPrice, minStock, maxStock
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
    public async Task<IActionResult> GetProdutoById(int id)
    {
        try
        {
            var produto = await _produtoService.GetProdutoByIdAsync(id);

            if (produto == null) return NotFound("Produto não encontrado.");

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
    public async Task<IActionResult> AddProduto(ProdutoDTO produtoDTO)
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
            var createdProduto = await _produtoService.AddProdutoAsync(produto);

            if (!createdProduto)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar cadastrar o produto no banco de dados.");
            }
            
            var produtoModelView = new ProdutoModelView
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                QuantidadeEstoque = produto.QuantidadeEstoque
            };

            return CreatedAtAction(nameof(GetProdutoById), new { id = produto.Id }, produtoModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar cadastrar o produto.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpPut("Atualizar/{id}")]
    public async Task<IActionResult> UpdateProduto(int id, ProdutoDTO produtoDTO)
    {
        if (!ValidateProdutoDTO(produtoDTO, out var errors))
        {
            return BadRequest(new ValidationErrors { Messages = errors });
        }

        try
        {
            var produto = await _produtoService.GetProdutoByIdAsync(id);

            if (produto == null) return NotFound("Produto não encontrado.");

            produto.Nome = produtoDTO.Nome;
            produto.Descricao = produtoDTO.Descricao;
            produto.Preco = produtoDTO.Preco;
            produto.QuantidadeEstoque = produtoDTO.QuantidadeEstoque;

            var updatedProduto = await _produtoService.UpdateProdutoAsync(produto);
            
            if (!updatedProduto)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar atualizar o produto no banco de dados.");
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
            _logger.LogError(ex, "Ocorreu um erro ao tentar atualizar o produto.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpDelete("Excluir/{id}")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        try
        {
            var produto = await _produtoService.GetProdutoByIdAsync(id);

            if (produto == null) return NotFound("Produto não encontrado.");

            var deletedProduto = await _produtoService.DeleteProdutoAsync(produto);

            if (!deletedProduto)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar deletar o produto no banco de dados.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar deletar o produto.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }
}
