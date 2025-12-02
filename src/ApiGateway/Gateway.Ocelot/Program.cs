using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o arquivo de configuração compartilhado
var sharedSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "../../Common", "sharedsettings.json");
builder.Configuration.AddJsonFile(sharedSettingsPath, optional: false, reloadOnChange: true);

builder.Configuration
      .SetBasePath(builder.Environment.ContentRootPath)
      .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

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
    options.MapInboundClaims = false;
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
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware do Ocelot gerencia o roteamento do gateway
await app.UseOcelot();

await app.RunAsync();
