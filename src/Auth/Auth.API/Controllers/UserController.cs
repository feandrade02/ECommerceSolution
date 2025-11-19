using Auth.API.Domain.DTOs;
using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Domain.Interfaces;
using Auth.API.Domain.ModelViews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    public UserController(ILogger<UserController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    private static bool ValidateCredentials(string email, string password, out List<string> errors, Roles? role = null)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("O email é obrigatório e não pode estar vazio.");
        }
        else if (email.Length < 12)
        {
            errors.Add("O email deve ter no mínimo 12 caracteres.");
        }
        else
        {
            // Validação do formato do email usando MailAddress
            // MailAddress valida automaticamente o formato: usuario@dominio.tld
            try
            {
                var mailAddress = new MailAddress(email);
                // MailAddress já valida o formato, então se chegou aqui, o email é válido
            }
            catch (FormatException)
            {
                errors.Add("O email deve ter o formato válido: usuario@dominio.tld");
            }
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("A senha (password) não pode ser vazia.");
        }
        else if (password.Length < 8)
        {
            errors.Add("A senha (password) deve ter no mínimo 8 caracteres.");
        }

        if (role != null && !Enum.IsDefined(typeof(Roles), role))
        {
            errors.Add("O cargo (role) fornecido é inválido.");
        }

        return errors.Count == 0;
    }

    [HttpGet("ObterTodos")]
    public async Task<IActionResult> GetAllUsers(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? email = null,
        Roles? role = null,
        bool ascending = true
    )
    {
        var validationErrors = new ValidationErrors{ Messages = [] };

        if (page <= 0)
        {
            validationErrors.Messages.Add("O número da página deve ser maior que zero.");
        }

        if (pageSize <= 0)
        {
            validationErrors.Messages.Add("O tamanho da página deve ser maior que zero.");
        }

        if (!string.IsNullOrEmpty(sortBy) && !string.Equals(sortBy, "email"))
        {
            validationErrors.Messages.Add("O campo sortBy deve ser 'email' ou vazio.");
        }

        if (validationErrors.Messages.Count > 0)
        {
            return BadRequest(validationErrors);
        }

        try
        {
            var users = await _userService.GetAllUsersAsync(page, pageSize, sortBy, email, role, ascending);

            var usersModelView = users.Select(u => new UserModelView
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            }).ToList();

            return Ok(usersModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar obter os usuários.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpGet("ObterPorId/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null) return NotFound("Usuário não encontrado.");

            var userModelView = new UserModelView
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            };
            return Ok(userModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar obter o usuário.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpPost("Cadastrar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddUser(UserDTO userDTO)
    {
        if (!ValidateCredentials(userDTO.Email, userDTO.Password, out var errors, userDTO.Role))
        {
            return BadRequest(new ValidationErrors { Messages = errors });
        }

        try
        {
            var existingUserEmail = await _userService.GetUserByEmailAsync(userDTO.Email);

            if (existingUserEmail != null) return StatusCode(StatusCodes.Status409Conflict, "O email solicitado já está cadastrado.");
            
            var user = new User
            {
                Email = userDTO.Email,
                Password = userDTO.Password,
                Role = userDTO.Role
            };

            var createdUser = await _userService.AddUserAsync(user);
            
            if (!createdUser)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar cadastrar o usuário no banco de dados.");
            }
            
            var userModelView = new UserModelView
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar cadastrar o usuário.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpPut("Atualizar/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, UserDTO userDTO)
    {
        if (!ValidateCredentials(userDTO.Email, userDTO.Password, out var errors, userDTO.Role))
        {
            return BadRequest(new ValidationErrors { Messages = errors });
        }

        try
        {
            var existingUserEmail = await _userService.GetUserByEmailAsync(userDTO.Email);

            if (existingUserEmail != null && existingUserEmail.Id != id) return StatusCode(StatusCodes.Status409Conflict, "O email solicitado já está cadastrado.");
            
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null) return NotFound("Usuário não encontrado.");

            user.Email = userDTO.Email;
            user.Password = userDTO.Password;
            user.Role = userDTO.Role;

            var updatedUser = await _userService.UpdateUserAsync(user);

            if (!updatedUser)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar atualizar o usuário no banco de dados.");
            }

            var userModelView = new UserModelView
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            };

            return Ok(userModelView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar atualizar o usuário.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpDelete("Excluir/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null) return NotFound("Usuário não encontrado.");

            var deletedUser = await _userService.DeleteUserAsync(user);

            if (!deletedUser)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao tentar excluir o usuário no banco de dados.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao tentar excluir o usuário.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado no servidor. Tente novamente mais tarde.");
        }
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        if (!ValidateCredentials(loginDTO.Email, loginDTO.Password, out var errors))
        {
            return BadRequest(new ValidationErrors { Messages = errors });
        }

        var userToLogin = await _userService.Login(loginDTO.Email, loginDTO.Password);

        if(userToLogin == null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, "Email ou senha (password) inválidos. Tente novamente.");
        }
        
        var token = _userService.GenerateJwtToken(userToLogin);
        
        return Ok(new LoggedUserModelView
        {
            Email = userToLogin.Email,
            Role = userToLogin.Role,
            AccessToken = token
        });
    }
}
