using Auth.API.Context;
using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UsersContext _context;

    public UserRepository(UsersContext context)
    {
        _context = context;
    }
    public async Task AddUserAsync(User user)
    {
        var now = DateTime.UtcNow;
        user.CreatedAt = now;
        user.UpdatedAt = now;
        await _context.Users.AddAsync(user);
    }

    public Task DeleteUserAsync(User user)
    {
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<List<User>> GetAllUsersAsync(
        int page = 1, 
        int pageSize = 10,
        string? sortBy = null,
        string? email = null, 
        Roles? role = null, 
        bool ascending = true)
    {
        var query = _context.Users.AsNoTracking().Where(i => !i.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(i => i.Email.Contains(email));
        }

        if (role.HasValue && role != null)
        {
            query = query.Where(i => i.Role == role);
        }

        query = (sortBy ?? string.Empty).ToLower() switch
        {
            "email" => ascending ? query.OrderBy(i => i.Email) : query.OrderByDescending(i => i.Email),
            _ => ascending ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt),
        };

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(i => i.Email == email && !i.IsDeleted);
        return user;
    }

    public Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<User?> Login(string email, string password)
    {
        var user = await _context.Users.SingleOrDefaultAsync(i => i.Email == email && i.Password == password && !i.IsDeleted);
        return user;
    }
    
    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}

