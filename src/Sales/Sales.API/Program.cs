using Microsoft.EntityFrameworkCore;
using Sales.API.Context;
using Sales.API.Domain.Interfaces;
using Sales.API.Domain.Services;
using RabbitMQ.Client;
using System.Net.Http.Headers;
using Sales.API.Repositories;
using Sales.API.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o arquivo de configuração compartilhado
var sharedSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "../../Common", "sharedsettings.json");
builder.Configuration.AddJsonFile(sharedSettingsPath, optional: false, reloadOnChange: true);

builder.Services.AddDbContext<SalesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StandardConnection")));

// Registra o IHttpContextAccessor para permitir o acesso ao HttpContext atual
builder.Services.AddHttpContextAccessor();

// Registra o handler de autenticação como um serviço transitório
builder.Services.AddTransient<HttpAuthHandler>();

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
})
.AddHttpMessageHandler<HttpAuthHandler>(); // Adiciona o handler ao pipeline do HttpClient

// Registra as dependências do seu serviço e repositório
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();

// Configura a conexão com o RabbitMQ
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

// Configura autenticação JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada.");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer não configurado.");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience não configurado.");

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var errorMessage = "Token de autenticação não fornecido ou inválido.";
            if (context.AuthenticateFailure is SecurityTokenExpiredException)
            {
                errorMessage = "O token JWT fornecido expirou. Por favor, faça login novamente.";
                context.Response.Headers.Append("Token-Expired", "true");
            }
            
            var errorResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Não autorizado",
                message = errorMessage
            });
            await context.Response.WriteAsync(errorResponse);
        }
    };
});

builder.Services.AddAuthorization();

// Adiciona serviços ao container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Adiciona o conversor para que enums sejam serializados/desserializados como strings
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sales API",
        Version = "v1.0.0",
        Description = "API para gerenciamento de vendas e pedidos"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu token}"
    });

    // Adiciona o requisito de segurança para que o Swagger use o token em todas as requisições
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Adiciona o serviço de Health Checks e configura a verificação do SQL Server
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("StandardConnection"));

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseAuthentication();
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
