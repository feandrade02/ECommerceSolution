using Contracts;
using RabbitMQ.Client;
using Sales.API.Domain.DTOs;
using Sales.API.Domain.Entities;
using Sales.API.Domain.Enums;
using Sales.API.Domain.Interfaces;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Sales.API.Domain.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(IPedidoRepository pedidoRepository, IHttpClientFactory httpClientFactory, ILogger<PedidoService> logger, IConnection rabbitConnection)
    {
        _pedidoRepository = pedidoRepository;
        _httpClientFactory = httpClientFactory;
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    public async Task<List<Pedido>> GetAllPedidosAsync(GetAllPedidosDTO request)
    {
        return await _pedidoRepository.GetAllPedidosAsync(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.Ascending,
            request.Status,
            request.MinTotalValue,
            request.MaxTotalValue
        );
    }

    public async Task<Pedido> GetPedidoByIdAsync(int id)
    {
        return await _pedidoRepository.GetPedidoByIdAsync(id);
    }

    public async Task<Pedido> AddPedidoAsync(PedidoDTO pedidoDTO)
    {
        if (pedidoDTO.Itens == null || pedidoDTO.Itens.Count == 0)
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
                throw new InvalidOperationException($"Não foi possível obter informações do produto com ID {itemDto.IdProduto}.");
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
            Status = StatusPedido.Confirmado,
            Itens = itensPedido
        };

        // 5. Persistir no banco
        try
        {
            await _pedidoRepository.AddPedidoAsync(pedido);
            await _pedidoRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro de banco de dados ao persistir o pedido. Verifique a exceção interna para detalhes.");
            throw new InvalidOperationException("Não foi possível processar o pedido devido a um problema com o banco de dados.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao persistir pedido no banco de dados.");
            throw new InvalidOperationException("Não foi possível processar o pedido no momento. Tente novamente mais tarde.");
        }

        // 6. Publicar evento para a API de Estoque dar baixa nos itens.
        var evento = new PedidoCriadoEvent
        {
            CorrelationId = Guid.NewGuid(), // Identificador único para esta transação
            Itens = [.. pedido.Itens.Select(item => new ItemPedidoReferenceDTO
            {
                IdProduto = item.IdProduto,
                Quantidade = item.Quantidade
            })]
        };

        try
        {
            using var channel = await _rabbitConnection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "pedido_criado_queue", durable: true, exclusive: false,
                autoDelete: false, arguments: null);
            
            var jsonString = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(jsonString);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "pedido_criado_queue", mandatory: true,
                basicProperties: properties, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem do PedidoCriadoEvent no RabbitMQ.");
        }
        _logger.LogInformation("Evento PedidoCriadoEvent publicado com CorrelationId: {CorrelationId}", evento.CorrelationId);

        return pedido;
    }
}
