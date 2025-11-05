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

    public async Task<List<Pedido>> GetAllPedidosAsync(
        int page,
        int pageSize,
        string sortBy,
        bool ascending,
        int? minTotalValue,
        int? maxTotalValue
    )
    {
        return await _pedidoRepository.GetAllPedidosAsync(
            page, pageSize, sortBy, ascending, minTotalValue, maxTotalValue
        );
    }

    public async Task<Pedido> GetPedidoByIdAsync(int id)
    {
        return await _pedidoRepository.GetPedidoByIdAsync(id);
    }

    // Método auxiliar para obter informações do produto na API de Estoque
    private async Task<ProdutoInfoDTO> GetProdutoInfo(HttpClient httpClient, int idProduto)
    {
        ProdutoInfoDTO produtoInfo;
        try
        {
            produtoInfo = await httpClient.GetFromJsonAsync<ProdutoInfoDTO>($"api/Produto/ObterPorId/{idProduto}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto com ID {ProdutoId} na API de Estoque.", idProduto);
            throw new InvalidOperationException($"Não foi possível obter informações do produto com ID {idProduto}.");
        }

        if (produtoInfo == null)
        {
            throw new KeyNotFoundException($"Produto com ID {idProduto} não encontrado.");
        }
        return produtoInfo;
    }

    public async Task<Pedido> CreatePedidoFromDTOAsync(PedidoDTO pedidoDTO)
    {
        var httpClient = _httpClientFactory.CreateClient("StockAPI");
        var itensPedido = new List<ItemPedido>();
        decimal valorTotal = 0;

        foreach (var itemDto in pedidoDTO.Itens)
        {
            ProdutoInfoDTO produtoInfo = await GetProdutoInfo(httpClient, itemDto.IdProduto);

            if (produtoInfo.QuantidadeEstoque < itemDto.Quantidade)
            {
                throw new InvalidOperationException($"Estoque insuficiente para o produto '{produtoInfo.Nome}'. Disponível: {produtoInfo.QuantidadeEstoque}, Solicitado: {itemDto.Quantidade}.");
            }

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

        return new Pedido
        {
            IdCliente = pedidoDTO.IdCliente,
            ValorTotal = valorTotal,
            Status = StatusPedido.Confirmado,
            Itens = itensPedido
        };
    }

    public async Task<bool> AddPedidoAsync(Pedido pedido)
    {
        // Publicar evento para a API de Estoque dar baixa nos itens.
        var evento = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(), // Identificador único para esta transação
            Itens = [.. pedido.Itens.Select(item => new ItemPedidoReference
            {
                IdProduto = item.IdProduto,
                Quantidade = -item.Quantidade
            })]
        };

        try
        {
            using var channel = await _rabbitConnection.CreateChannelAsync();
            const string queueName = "update_stock_queue";

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false,
                autoDelete: false, arguments: null);
            
            var jsonString = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(jsonString);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true,
                basicProperties: properties, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem do UpdateStockEvent no RabbitMQ.");
        }
        _logger.LogInformation("Evento UpdateStockEvent publicado com CorrelationId: {CorrelationId}", evento.CorrelationId);

        try
        {
            await _pedidoRepository.AddPedidoAsync(pedido);
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
        
        return await _pedidoRepository.SaveChangesAsync();
    }

    public async Task<bool> UpdatePedidoAsync(Pedido pedido)
    {
        await _pedidoRepository.UpdatePedidoAsync(pedido);
        return await _pedidoRepository.SaveChangesAsync();
    }

    public async Task<bool> DeletePedidoAsync(Pedido pedido)
    {
        // Publicar evento para a API de Estoque restaurar os itens.
        var evento = new UpdateStockEvent
        {
            CorrelationId = Guid.NewGuid(),
            Itens = [.. pedido.Itens
                .Where(item => !item.IsDeleted)
                .Select(item => new ItemPedidoReference
                {
                    IdProduto = item.IdProduto,
                    Quantidade = item.Quantidade
                })]
        };

        try
        {
            using var channel = await _rabbitConnection.CreateChannelAsync();
            const string queueName = "update_stock_queue";

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false,
                autoDelete: false, arguments: null);

            var jsonString = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(jsonString);

            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true,
                basicProperties: properties, body: body);

            _logger.LogInformation("Evento de restauração de estoque (UpdateStockEvent) publicado com CorrelationId: {CorrelationId}", evento.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem de restauração de estoque no RabbitMQ. A deleção do pedido continuará, mas o estoque pode ficar inconsistente.");
        }

        try
        {
            await _pedidoRepository.DeletePedidoAsync(pedido);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro de banco de dados ao excluir o pedido. Verifique a exceção interna para detalhes.");
            throw new InvalidOperationException("Não foi possível excluir o pedido devido a um problema com o banco de dados.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir pedido do banco de dados.");
            throw new InvalidOperationException("Não foi possível excluir o pedido no momento. Tente novamente mais tarde.");
        }
        
        return await _pedidoRepository.SaveChangesAsync();
    }
}
