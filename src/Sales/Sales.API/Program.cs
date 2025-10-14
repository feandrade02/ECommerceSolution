using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.Services;
using Sales.API.Validation;
using RabbitMQ.Client;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SalesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StandardConnection")));

// Registra o HttpClientFactory e configura um cliente nomeado para a Stock.API
builder.Services.AddHttpClient("StockAPI", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:StockAPI"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new InvalidOperationException("A URL base para a StockAPI não foi encontrada na configuração (ServiceUrls:StockAPI).");
    }
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Registra as dependências do seu serviço e repositório
builder.Services.AddScoped<IPedidoRepository, Sales.API.Repositories.PedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Configura a conexão com o RabbitMQ como um singleton
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
using var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

// Registra o FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<GetAllPedidosDTOValidator>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers(); // A validação automática é habilitada por padrão em [ApiController]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adiciona o serviço de Health Checks e configura a verificação do SQL Server
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("StandardConnection"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Mapeia o endpoint de Health Checks para a rota /health
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
