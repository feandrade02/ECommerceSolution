using Microsoft.AspNetCore.Mvc;

namespace Stock.API.Controllers
{
    [ApiController]
    [Route("/")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Bem-vindo à API de Estoque! Acesse /swagger para ver os endpoints disponíveis.");
        }
    }
}
