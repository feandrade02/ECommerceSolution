using Microsoft.EntityFrameworkCore;
using Stock.API.Interfaces;
using Stock.API.Repositories;
using Stock.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StockContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StandardConnection")));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

// Mapeia o endpoint de Health Checks para a rota /health
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
