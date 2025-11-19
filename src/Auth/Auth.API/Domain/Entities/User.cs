using Auth.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Auth.API.Domain.Entities;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    [MinLength(12)]
    [MaxLength(100)]
    public string Email { get; set; } = default!;
    
    [Required]
    [MinLength(8)]
    [MaxLength(20)]
    public string Password { get; set; } = default!;
    
    [Required]
    [EnumDataType(typeof(Roles))]
    public Roles Role { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

