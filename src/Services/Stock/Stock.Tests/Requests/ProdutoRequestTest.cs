using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stock.API.Domain.DTOs;
using Stock.API.Interfaces;
using Stock.API.ModelViews;
using Stock.Context;
using System.Net;
using System.Net.Http.Json;

namespace Stock.Tests.Requests
{
    public class ProdutoRequestTest : IDisposable
    {
        private readonly HttpClient _client;
        private readonly IHost _host;

        public ProdutoRequestTest()
        {
            // 1. Cria um HostBuilder para ter controle total sobre a configuração.
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                // 2. Usa o TestServer como o servidor web em memória.
                webHost.UseTestServer();

                // 3. Configura os serviços ANTES da aplicação principal.
                webHost.ConfigureServices(services =>
                {
                    // Remove qualquer registro prévio de DbContext, se houver (garantia extra).
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<StockContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Adiciona o DbContext com o provedor InMemory.
                    services.AddDbContext<StockContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });

                    // Adiciona os controllers da API de Stock para que o roteamento funcione no teste.
                    services.AddControllers().AddApplicationPart(typeof(API.Controllers.ProdutoController).Assembly);

                    // Registra as dependências necessárias, como o repositório.
                    services.AddScoped<IProdutoRepository, API.Repositories.ProdutoRepository>();
                });

                // 4. Configura o pipeline de requisições da aplicação (semelhante ao Program.cs).
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });
            });

            // 5. Constrói o host e obtém o cliente HTTP.
            _host = hostBuilder.Start();
            _client = _host.GetTestClient();

            // 6. Prepara o banco de dados para um estado limpo.
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StockContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _client.Dispose();
            _host.Dispose();
        }

        [Fact]
        public async Task PostProduct_Then_GetById_ReturnsCorrectProduct()
        {
            // Arrange: Criar um novo produto
            var newProductDto = new ProdutoDTO
            {
                Nome = "Produto de Teste E2E",
                Preco = 199.99m,
                QuantidadeEstoque = 50
            };

            // Act (POST): Envia a requisição para cadastrar o produto
            var postResponse = await _client.PostAsJsonAsync("/api/Produto/Cadastrar", newProductDto);
            
            // Assert (POST): Verifica se o produto foi criado com sucesso
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var createdProduct = await postResponse.Content.ReadFromJsonAsync<ProdutoModelView>();
            Assert.NotNull(createdProduct);
            Assert.Equal(newProductDto.Nome, createdProduct.Nome);

            // Arrange (GET): Usa o ID do produto criado para a próxima requisição
            var productId = createdProduct.Id;

            // Act (GET): Envia a requisição para obter o produto pelo ID
            var getResponse = await _client.GetAsync($"/api/Produto/ObterPorId/{productId}");

            // Assert (GET): Verifica se o produto retornado é o correto
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var fetchedProduct = await getResponse.Content.ReadFromJsonAsync<ProdutoModelView>();
            Assert.NotNull(fetchedProduct);
            Assert.Equal(productId, fetchedProduct.Id);
            Assert.Equal(newProductDto.Nome, fetchedProduct.Nome);
        }

        [Fact]
        public async Task GetProductById_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = 9999;

            // Act
            var response = await _client.GetAsync($"/api/Produto/ObterPorId/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostProduct_InvalidData_ReturnsBadRequest()
        {
            // Arrange: DTO com dados inválidos (nome em branco)
            var invalidProductDto = new ProdutoDTO
            {
                Nome = "",
                Preco = 10,
                QuantidadeEstoque = 5
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Produto/Cadastrar", invalidProductDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorContent = await response.Content.ReadFromJsonAsync<ValidationErrors>();
            Assert.Contains("O nome do produto é obrigatório.", errorContent.Messages);
        }

        [Fact]
        public async Task UpdateProduct_ExistingIdAndValidData_ReturnsOkAndUpdatesProduct()
        {
            // Arrange: Cria um produto inicial
            var initialDto = new ProdutoDTO { Nome = "Produto Original", Preco = 100, QuantidadeEstoque = 10 };
            var postResponse = await _client.PostAsJsonAsync("/api/Produto/Cadastrar", initialDto);
            var createdProduct = await postResponse.Content.ReadFromJsonAsync<ProdutoModelView>();
            var productId = createdProduct.Id;

            var updatedDto = new ProdutoDTO { Nome = "Produto Atualizado", Preco = 150, QuantidadeEstoque = 20 };

            // Act: Envia a requisição para atualizar o produto
            var updateResponse = await _client.PutAsJsonAsync($"/api/Produto/Atualizar/{productId}", updatedDto);

            // Assert (PUT): Verifica se a atualização foi bem-sucedida
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProdutoModelView>();
            Assert.Equal(updatedDto.Nome, updatedProduct.Nome);
            Assert.Equal(updatedDto.Preco, updatedProduct.Preco);

            // Act (GET): Busca o produto novamente para confirmar a persistência da alteração
            var getResponse = await _client.GetAsync($"/api/Produto/ObterPorId/{productId}");
            var fetchedProduct = await getResponse.Content.ReadFromJsonAsync<ProdutoModelView>();

            // Assert (GET): Verifica se os dados foram realmente atualizados no banco
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(updatedDto.Nome, fetchedProduct.Nome);
        }

        [Fact]
        public async Task UpdateProduct_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = 9999;
            var dto = new ProdutoDTO { Nome = "Qualquer Nome", Preco = 10, QuantidadeEstoque = 10 };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Produto/Atualizar/{nonExistentId}", dto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProduct_ExistingId_ReturnsNoContentAndDeletesProduct()
        {
            // Arrange: Cria um produto para deletar
            var dto = new ProdutoDTO { Nome = "Produto a ser Deletado", Preco = 10, QuantidadeEstoque = 1 };
            var postResponse = await _client.PostAsJsonAsync("/api/Produto/Cadastrar", dto);
            var createdProduct = await postResponse.Content.ReadFromJsonAsync<ProdutoModelView>();
            var productId = createdProduct.Id;

            // Act: Envia a requisição de deleção
            var deleteResponse = await _client.DeleteAsync($"/api/Produto/Deletar/{productId}");

            // Assert (DELETE): Verifica se a resposta é NoContent
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Act (GET): Tenta buscar o produto deletado
            var getResponse = await _client.GetAsync($"/api/Produto/ObterPorId/{productId}");

            // Assert (GET): Verifica se o produto não é mais encontrado (NotFound)
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteProduct_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = 9999;

            // Act
            var response = await _client.DeleteAsync($"/api/Produto/Deletar/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProducts_WithFilterAndPagination_ReturnsFilteredAndPaginatedList()
        {
            // Arrange: Adiciona vários produtos para teste de filtro e paginação
            await _client.PostAsJsonAsync("/api/Produto/Cadastrar", new ProdutoDTO { Nome = "Caneta Azul", Preco = 2, QuantidadeEstoque = 100 });
            await _client.PostAsJsonAsync("/api/Produto/Cadastrar", new ProdutoDTO { Nome = "Caneta Preta", Preco = 2, QuantidadeEstoque = 50 });
            await _client.PostAsJsonAsync("/api/Produto/Cadastrar", new ProdutoDTO { Nome = "Lápis", Preco = 1, QuantidadeEstoque = 200 });
            await _client.PostAsJsonAsync("/api/Produto/Cadastrar", new ProdutoDTO { Nome = "Caderno Grande", Preco = 25, QuantidadeEstoque = 20 });

            // Act: Busca por produtos que contenham "Caneta", com preço máximo de 10, na primeira página
            var response = await _client.GetAsync("/api/Produto/ObterTodos?name=Caneta&maxPrice=10&page=1&pageSize=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<List<ProdutoModelView>>();
            Assert.NotNull(products);
            Assert.Equal(2, products.Count); // Deve encontrar "Caneta Azul" e "Caneta Preta"
            Assert.Contains(products, p => p.Nome == "Caneta Azul");
            Assert.Contains(products, p => p.Nome == "Caneta Preta");
            Assert.DoesNotContain(products, p => p.Nome == "Lápis"); // Não corresponde ao filtro de nome
            Assert.DoesNotContain(products, p => p.Nome == "Caderno Grande"); // Não corresponde ao filtro de preço
        }

        [Fact]
        public async Task GetAllProducts_WithInvalidParameters_ReturnsBadRequest()
        {
            // Arrange: Define URLs com parâmetros inválidos
            var invalidPageUrl = "/api/Produto/ObterTodos?page=0";
            var invalidPriceRangeUrl = "/api/Produto/ObterTodos?minPrice=100&maxPrice=50";

            // Act
            var pageResponse = await _client.GetAsync(invalidPageUrl);
            var priceResponse = await _client.GetAsync(invalidPriceRangeUrl);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, pageResponse.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, priceResponse.StatusCode);
            var priceError = await priceResponse.Content.ReadFromJsonAsync<ValidationErrors>();
            Assert.Contains("O preço mínimo não pode ser maior que o preço máximo.", priceError.Messages);
            var pageError = await pageResponse.Content.ReadFromJsonAsync<ValidationErrors>();
            Assert.Contains("O número da página deve ser maior que zero.", pageError.Messages);
        }
    }
}