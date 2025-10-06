using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stock.API.Controllers;
using Stock.API.Domain.DTOs;
using Stock.API.Interfaces;
using Stock.API.ModelViews;
using Stock.Domain.Entities;

namespace Stock.Tests.Controllers
{
    public class ProdutoControllerTests
    {
        private readonly Mock<IProdutoRepository> _mockRepo;
        private readonly Mock<ILogger<ProdutoController>> _mockLogger;
        private readonly ProdutoController _controller;

        public ProdutoControllerTests()
        {
            _mockRepo = new Mock<IProdutoRepository>();
            _mockLogger = new Mock<ILogger<ProdutoController>>();
            _controller = new ProdutoController(_mockRepo.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetProductById_ExistingId_ReturnsOkObjectResult()
        {
            // Arrange
            var produto = new Produto { Id = 1, Nome = "Teste", Preco = 10, QuantidadeEstoque = 5 };
            _mockRepo.Setup(repo => repo.GetProductById(1)).ReturnsAsync(produto);

            // Act
            var result = await _controller.GetProductById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsAssignableFrom<ProdutoModelView>(okResult.Value);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public async Task GetProductById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetProductById(It.IsAny<int>())).ReturnsAsync((Produto)null);

            // Act
            var result = await _controller.GetProductById(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetProductById_RepositoryThrowsException_Returns500()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetProductById(It.IsAny<int>())).ThrowsAsync(new Exception());

            // Act
            var result = await _controller.GetProductById(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task AddProduct_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            var produtoDto = new ProdutoDTO
            {
                Nome = "Novo Produto",
                Preco = 25.50m,
                QuantidadeEstoque = 10
            };
            _mockRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.AddProduct(produtoDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(ProdutoController.GetProductById), createdAtActionResult.ActionName);
            var model = Assert.IsAssignableFrom<ProdutoModelView>(createdAtActionResult.Value);
            Assert.Equal("Novo Produto", model.Nome);
        }

        [Fact]
        public async Task AddProduct_InvalidDto_ReturnsBadRequest()
        {
            // Arrange
            var produtoDto = new ProdutoDTO { Nome = "", Preco = 0 };

            // Act
            var result = await _controller.AddProduct(produtoDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<ValidationErrors>(badRequestResult.Value);
            Assert.Contains("O nome do produto é obrigatório.", errors.Messages);
            Assert.Contains("O preço do produto deve ser maior que zero.", errors.Messages);
        }

        [Fact]
        public async Task UpdateProduct_ExistingIdAndValidDto_ReturnsOk()
        {
            // Arrange
            var produtoId = 1;
            var existingProduto = new Produto { Id = produtoId, Nome = "Antigo", Preco = 10, QuantidadeEstoque = 5 };
            var produtoDto = new ProdutoDTO { Nome = "Atualizado", Preco = 15, QuantidadeEstoque = 8 };

            _mockRepo.Setup(repo => repo.GetProductById(produtoId)).ReturnsAsync(existingProduto);
            _mockRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateProduct(produtoId, produtoDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsAssignableFrom<ProdutoModelView>(okResult.Value);
            Assert.Equal("Atualizado", model.Nome);
            Assert.Equal(15, model.Preco);
        }

        [Fact]
        public async Task UpdateProduct_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var produtoId = 99;
            var produtoDto = new ProdutoDTO { Nome = "Atualizado", Preco = 15, QuantidadeEstoque = 8 };
            _mockRepo.Setup(repo => repo.GetProductById(produtoId)).ReturnsAsync((Produto)null);

            // Act
            var result = await _controller.UpdateProduct(produtoId, produtoDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_ExistingId_ReturnsNoContent()
        {
            // Arrange
            var produtoId = 1;
            var existingProduto = new Produto { Id = produtoId, Nome = "Para Deletar" };
            _mockRepo.Setup(repo => repo.GetProductById(produtoId)).ReturnsAsync(existingProduto);
            _mockRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProduct(produtoId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var produtoId = 99;
            _mockRepo.Setup(repo => repo.GetProductById(produtoId)).ReturnsAsync((Produto)null);

            // Act
            var result = await _controller.DeleteProduct(produtoId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAllProducts_WithValidations_ReturnsBadRequest()
        {
            // Arrange & Act
            var resultPage = await _controller.GetAllProducts(page: 0); 
            var resultPageSize = await _controller.GetAllProducts(pageSize: 0);
            var resultPrice = await _controller.GetAllProducts(minPrice: 100, maxPrice: 50); 
            var resultStock = await _controller.GetAllProducts(minStock: 100, maxStock: 50);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultPage);
            Assert.IsType<BadRequestObjectResult>(resultPageSize);
            Assert.IsType<BadRequestObjectResult>(resultPrice);
            Assert.IsType<BadRequestObjectResult>(resultStock);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsOkWithProducts()
        {
            // Arrange
            var produtos = new List<Produto>
            {
                new Produto { Id = 1, Nome = "Produto A" },
                new Produto { Id = 2, Nome = "Produto B" }
            };
            _mockRepo.Setup(repo => repo.GetAllProducts(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(produtos);

            // Act
            var result = await _controller.GetAllProducts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsAssignableFrom<List<ProdutoModelView>>(okResult.Value);
            Assert.Equal(2, model.Count);
        }
    }
}



