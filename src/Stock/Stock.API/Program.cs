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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Stock API",
        Version = "v1.0.0",
        Description = "API para gerenciamento de estoque e produtos"
    });
});

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

// Mapeia o endpoint de Health Checks para a rota /health (para uso interno)
app.MapHealthChecks("/health");

// Endpoint customizado para o Swagger
app.MapGet("/api/health", async (Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) =>
{
    var healthReport = await healthCheckService.CheckHealthAsync();
    return healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
        ? Results.Ok(new { status = "Healthy", checks = healthReport.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }) })
        : Results.StatusCode(503);
})
.WithName("HealthCheck")
.WithTags("Health")
.Produces(200)
.Produces(503);

app.MapControllers();

app.Run();
