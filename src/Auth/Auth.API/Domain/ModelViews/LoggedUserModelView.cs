using Auth.API.Domain.Enums;

namespace Auth.API.Domain.ModelViews;

public record LoggedUserModelView
{
    public string Email { get; set; } = default!;
    public Roles Role { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
}

