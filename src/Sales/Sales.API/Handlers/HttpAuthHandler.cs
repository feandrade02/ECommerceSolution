using System.Net.Http.Headers;

namespace Sales.API.Handlers;

// Um DelegatingHandler que intercepta requisições HTTP de saída
// e propaga o token de autenticação JWT da requisição de entrada.
public class HttpAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tenta obter o token do cabeçalho "Authorization" da requisição original
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(token))
        {
            // Adiciona o mesmo cabeçalho de autorização à requisição de saída
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
