using Microsoft.AspNetCore.Mvc;
using Stock.API.Domain.DTOs;
using Stock.API.Interfaces;
using Stock.Domain.Entities;

namespace Stock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutoController : ControllerBase
    {
        private readonly IProdutoRepository _produtoRepository;

        public ProdutoController(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        // TODO: Fazer validações

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
            var produtos = await _produtoRepository.GetAllProducts(
                page, pageSize, name, ascending, minPrice, maxPrice, minStock, maxStock
            );

            return Ok(produtos);
        }

        [HttpGet("ObterPorId/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var produto = await _produtoRepository.GetProductById(id);

            if (produto == null)
            {
                return NotFound("Produto não encontrado.");
            }

            return Ok(produto);
        }

        [HttpPost("Cadastrar")]
        public async Task<IActionResult> AddProduct(ProdutoDTO produtoDTO)
        {
            var produto = new Produto
            {
                Nome = produtoDTO.Nome,
                Descricao = produtoDTO.Descricao,
                Preco = produtoDTO.Preco,
                QuantidadeEstoque = produtoDTO.QuantidadeEstoque
            };

            await _produtoRepository.AddProduct(produto);
            await _produtoRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = produto.Id }, produto);
        }

        [HttpPut("Atualizar/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProdutoDTO produtoDTO)
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

            return Ok(produto);
        }
        
        [HttpDelete("Deletar/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
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
    }
}