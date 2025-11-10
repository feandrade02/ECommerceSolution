using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.Services;
using RabbitMQ.Client;
using System.Net.Http.Headers;
using Sales.API.Repositories;

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
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();

// Configura a conexão com o RabbitMQ
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

// Add services to the container.
builder.Services.AddControllers(); // A validação automática é habilitada por padrão em [ApiController]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sales API",
        Version = "v1.0.0",
        Description = "API para gerenciamento de vendas e pedidos"
    });
});

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
