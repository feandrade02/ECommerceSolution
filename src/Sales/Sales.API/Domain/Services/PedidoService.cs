using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;

namespace Sales.API.Domain.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PedidoService> _logger;

        public PedidoService(IPedidoRepository pedidoRepository, IHttpClientFactory httpClientFactory, ILogger<PedidoService> logger)
        {
            _pedidoRepository = pedidoRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<Pedido> CriarPedidoAsync(PedidoDTO pedidoDTO)
        {
            if (pedidoDTO.Itens == null || !pedidoDTO.Itens.Any())
            {
                throw new ArgumentException("O pedido deve conter pelo menos um item.");
            }

            var httpClient = _httpClientFactory.CreateClient("StockAPI");
            var itensPedido = new List<ItemPedido>();
            decimal valorTotal = 0;

            foreach (var itemDto in pedidoDTO.Itens)
            {
                // 1. Buscar informações do produto na API de Estoque
                ProdutoInfoDTO produtoInfo;
                try
                {
                    produtoInfo = await httpClient.GetFromJsonAsync<ProdutoInfoDTO>($"api/Produto/ObterPorId/{itemDto.IdProduto}");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Erro ao buscar produto com ID {ProdutoId} na API de Estoque.", itemDto.IdProduto);
                    throw new InvalidOperationException($"Não foi possível obter informações do produto ID {itemDto.IdProduto}.");
                }

                if (produtoInfo == null)
                {
                    throw new KeyNotFoundException($"Produto com ID {itemDto.IdProduto} não encontrado.");
                }

                // 2. Validar estoque
                if (produtoInfo.QuantidadeEstoque < itemDto.Quantidade)
                {
                    throw new InvalidOperationException($"Estoque insuficiente para o produto '{produtoInfo.Nome}'. Disponível: {produtoInfo.QuantidadeEstoque}, Solicitado: {itemDto.Quantidade}.");
                }

                // 3. Criar o ItemPedido e calcular o subtotal
                var itemPedido = new ItemPedido
                {
                    IdProduto = itemDto.IdProduto,
                    NomeProduto = produtoInfo.Nome,
                    PrecoUnitario = produtoInfo.Preco,
                    Quantidade = itemDto.Quantidade
                };
                itensPedido.Add(itemPedido);
                valorTotal += itemPedido.PrecoUnitario * itemPedido.Quantidade;
            }

            // 4. Criar o Pedido
            var pedido = new Pedido
            {
                IdCliente = pedidoDTO.IdCliente,
                ValorTotal = valorTotal,
                Status = StatusPedido.Iniciado, // Status inicial definido pelo sistema
                Itens = itensPedido
            };

            // 5. Persistir no banco (o repositório cuidará disso)
            await _pedidoRepository.AddPedidoAsync(pedido);
            await _pedidoRepository.SaveChangesAsync();

            // TODO: Idealmente, aqui você publicaria um evento para a API de Estoque dar baixa nos itens.

            return pedido;
        }
    }
}
