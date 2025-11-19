using Auth.API.Domain.Enums;

namespace Auth.API.Domain.ModelViews;

public record UserModelView
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public Roles Role { get; set; } = default!;
}

