using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Stock.API.Domain.Interfaces;
using Stock.API.Domain.Services;
using Stock.API.Repositories;
using Stock.API.Context;
using Stock.API.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StockContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StandardConnection")));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registra as dependências do seu serviço e repositório
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();

builder.Services.AddHostedService<UpdateStockWorker>();

// Configura a conexão com o RabbitMQ
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

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
