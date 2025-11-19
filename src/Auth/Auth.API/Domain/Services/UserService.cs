using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auth.API.Domain.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    
    public UserService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<bool> AddUserAsync(User user)
    {
        await _userRepository.AddUserAsync(user);
        return await _userRepository.SaveChangesAsync();
    }

    public async Task<bool> DeleteUserAsync(User user)
    {
        await _userRepository.DeleteUserAsync(user);
        return await _userRepository.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllUsersAsync(
        int page, 
        int pageSize, 
        string? sortBy, 
        string? email, 
        Roles? role, 
        bool ascending
    )
    {
        return await _userRepository.GetAllUsersAsync(page, pageSize, sortBy, email, role, ascending);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepository.GetUserByIdAsync(id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetUserByEmailAsync(email);
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        await _userRepository.UpdateUserAsync(user);
        return await _userRepository.SaveChangesAsync();
    }

    public async Task<User?> Login(string email, string password)
    {
        var user = await _userRepository.Login(email, password);
        return user;
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer não configurado.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience não configurado.");
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = Encoding.UTF8.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

