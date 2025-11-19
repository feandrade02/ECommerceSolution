using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;

namespace Auth.API.Domain.Interfaces;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync(
        int page,
        int pageSize,
        string? sortBy,
        string? email,
        Roles? role,
        bool ascending
    );
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> Login(string email, string password);
    Task<bool> AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(User user);
    string GenerateJwtToken(User user);
}

