using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("/")]
[ApiExplorerSettings(IgnoreApi = true)]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Bem-vindo à API de Gerenciamento de usuários! Acesse /swagger para ver os endpoints disponíveis.");
    }
}
