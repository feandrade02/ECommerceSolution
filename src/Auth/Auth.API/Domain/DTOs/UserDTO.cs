using Auth.API.Domain.Enums;

namespace Auth.API.Domain.DTOs;

public record UserDTO
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public Roles Role { get; set; } = default!;
}

