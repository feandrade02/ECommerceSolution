using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;

namespace Auth.API.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? email = null,
        Roles? role = null,
        bool ascending = true
    );
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> Login(string email, string password);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(User user);
    Task<bool> SaveChangesAsync();
}

