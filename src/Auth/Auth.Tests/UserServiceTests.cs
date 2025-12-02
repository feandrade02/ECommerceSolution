using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Domain.Interfaces;
using Auth.API.Domain.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Auth.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _configurationMock = new Mock<IConfiguration>();
        
        var jwtSectionMock = new Mock<IConfigurationSection>();
        _configurationMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);
        
        var secretKeyMock = new Mock<IConfigurationSection>();
        secretKeyMock.Setup(s => s.Value).Returns("ThisIsAVerySecureSecretKeyWith32Characters!");
        jwtSectionMock.Setup(s => s["SecretKey"]).Returns("ThisIsAVerySecureSecretKeyWith32Characters!");
        
        var issuerMock = new Mock<IConfigurationSection>();
        issuerMock.Setup(s => s.Value).Returns("TestIssuer");
        jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        
        var audienceMock = new Mock<IConfigurationSection>();
        audienceMock.Setup(s => s.Value).Returns("TestAudience");
        jwtSectionMock.Setup(s => s["Audience"]).Returns("TestAudience");
        
        var expirationMock = new Mock<IConfigurationSection>();
        expirationMock.Setup(s => s.Value).Returns("60");
        jwtSectionMock.Setup(s => s["ExpirationMinutes"]).Returns("60");
        
        _userService = new UserService(_userRepositoryMock.Object, _configurationMock.Object);
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        var expectedUsers = new List<User> { new User { Id = 1, Email = "test@example.com" } };
        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Roles?>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10, null, null, null, true);

        // Assert
        Assert.Equal(expectedUsers, result);
        _userRepositoryMock.Verify(r => r.GetAllUsersAsync(1, 10, null, null, null, true), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Roles?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10, null, null, null, true);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldFilterByEmail_WhenEmailProvided()
    {
        // Arrange
        var expectedUsers = new List<User> { new User { Id = 1, Email = "admin@example.com" } };
        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), "admin", It.IsAny<Roles?>(), It.IsAny<bool>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10, null, "admin", null, true);

        // Assert
        Assert.Equal(expectedUsers, result);
        _userRepositoryMock.Verify(r => r.GetAllUsersAsync(1, 10, null, "admin", null, true), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldFilterByRole_WhenRoleProvided()
    {
        // Arrange
        var expectedUsers = new List<User> 
        { 
            new User { Id = 1, Email = "admin@example.com", Role = Roles.Admin } 
        };
        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), Roles.Admin, It.IsAny<bool>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _userService.GetAllUsersAsync(1, 10, null, null, Roles.Admin, true);

        // Assert
        Assert.Equal(expectedUsers, result);
        Assert.All(result, u => Assert.Equal(Roles.Admin, u.Role));
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Email = "test@example.com" };
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.Equal(expectedUser, result);
        _userRepositoryMock.Verify(r => r.GetUserByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetUserByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Email = "test@example.com" };
        _userRepositoryMock.Setup(r => r.GetUserByEmailAsync("test@example.com"))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.Equal(expectedUser, result);
        _userRepositoryMock.Verify(r => r.GetUserByEmailAsync("test@example.com"), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddUserAsync Tests

    [Fact]
    public async Task AddUserAsync_ShouldCallAddAndSave_OnRepository()
    {
        // Arrange
        var user = new User { Email = "newuser@example.com", Password = "password123", Role = Roles.Sales };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.AddUserAsync(user);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.AddUserAsync(user), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_ShouldReturnTrue_WhenSaveSucceeds()
    {
        // Arrange
        var user = new User { Email = "newuser@example.com", Password = "password123", Role = Roles.Sales };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.AddUserAsync(user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AddUserAsync_ShouldReturnFalse_WhenSaveFails()
    {
        // Arrange
        var user = new User { Email = "newuser@example.com", Password = "password123", Role = Roles.Sales };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await _userService.AddUserAsync(user);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ShouldCallUpdateAndSave_OnRepository()
    {
        // Arrange
        var user = new User { Id = 1, Email = "updated@example.com", Password = "newpassword", Role = Roles.Admin };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserAsync(user);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.UpdateUserAsync(user), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnTrue_WhenSaveSucceeds()
    {
        // Arrange
        var user = new User { Id = 1, Email = "updated@example.com", Password = "newpassword", Role = Roles.Admin };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUserAsync(user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnFalse_WhenSaveFails()
    {
        // Arrange
        var user = new User { Id = 1, Email = "updated@example.com", Password = "newpassword", Role = Roles.Admin };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await _userService.UpdateUserAsync(user);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ShouldCallDeleteAndSave_OnRepository()
    {
        // Arrange
        var user = new User { Id = 1, Email = "deleted@example.com" };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.DeleteUserAsync(user);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.DeleteUserAsync(user), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnTrue_WhenSaveSucceeds()
    {
        // Arrange
        var user = new User { Id = 1, Email = "deleted@example.com" };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await _userService.DeleteUserAsync(user);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalse_WhenSaveFails()
    {
        // Arrange
        var user = new User { Id = 1, Email = "deleted@example.com" };
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await _userService.DeleteUserAsync(user);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Email = "admin@example.com", Password = "password123", Role = Roles.Admin };
        _userRepositoryMock.Setup(r => r.Login("admin@example.com", "password123"))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.Login("admin@example.com", "password123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser, result);
        _userRepositoryMock.Verify(r => r.Login("admin@example.com", "password123"), Times.Once);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenCredentialsAreInvalid()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userService.Login("invalid@example.com", "wrongpassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.Login("test@example.com", "testpass"))
            .ReturnsAsync(new User());

        // Act
        await _userService.Login("test@example.com", "testpass");

        // Assert
        _userRepositoryMock.Verify(r => r.Login("test@example.com", "testpass"), Times.Once);
    }

    #endregion

    #region GenerateJwtToken Tests

    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken_WhenUserIsValid()
    {
        // Arrange
        var user = new User 
        { 
            Id = 1, 
            Email = "admin@example.com", 
            Role = Roles.Admin 
        };

        // Act
        var token = _userService.GenerateJwtToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Verifica que é um token JWT válido (possui 3 partes separadas por pontos)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateJwtToken_ShouldIncludeUserClaims_InToken()
    {
        // Arrange
        var user = new User 
        { 
            Id = 1, 
            Email = "admin@example.com", 
            Role = Roles.Admin 
        };

        // Act
        var token = _userService.GenerateJwtToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "admin@example.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == "nameid" && c.Value == "1");
        Assert.Contains(jwtToken.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void GenerateJwtToken_ShouldSetExpirationTime_Correctly()
    {
        // Arrange
        var user = new User 
        { 
            Id = 1, 
            Email = "admin@example.com", 
            Role = Roles.Admin 
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _userService.GenerateJwtToken(user);
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiration = beforeGeneration.AddMinutes(60);
        var actualExpiration = jwtToken.ValidTo;
        
        // Permite 1 minuto de tolerância para o tempo de execução do teste
        Assert.InRange(actualExpiration, expectedExpiration.AddMinutes(-1), afterGeneration.AddMinutes(61));
    }

    [Fact]
    public void GenerateJwtToken_ShouldThrowException_WhenSecretKeyIsMissing()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Role = Roles.Admin };
        
        var configMock = new Mock<IConfiguration>();
        var jwtSectionMock = new Mock<IConfigurationSection>();
        configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);
        jwtSectionMock.Setup(s => s["SecretKey"]).Returns((string)null);
        
        var service = new UserService(_userRepositoryMock.Object, configMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.GenerateJwtToken(user));
        Assert.Contains("JWT SecretKey não configurada", exception.Message);
    }

    [Fact]
    public void GenerateJwtToken_ShouldThrowException_WhenIssuerIsMissing()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Role = Roles.Admin };
        
        var configMock = new Mock<IConfiguration>();
        var jwtSectionMock = new Mock<IConfigurationSection>();
        configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);
        jwtSectionMock.Setup(s => s["SecretKey"]).Returns("ThisIsAVerySecureSecretKeyWith32Characters!");
        jwtSectionMock.Setup(s => s["Issuer"]).Returns((string)null);
        
        var service = new UserService(_userRepositoryMock.Object, configMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.GenerateJwtToken(user));
        Assert.Contains("JWT Issuer não configurado", exception.Message);
    }

    [Fact]
    public void GenerateJwtToken_ShouldThrowException_WhenAudienceIsMissing()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Role = Roles.Admin };
        
        var configMock = new Mock<IConfiguration>();
        var jwtSectionMock = new Mock<IConfigurationSection>();
        configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);
        jwtSectionMock.Setup(s => s["SecretKey"]).Returns("ThisIsAVerySecureSecretKeyWith32Characters!");
        jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSectionMock.Setup(s => s["Audience"]).Returns((string)null);
        
        var service = new UserService(_userRepositoryMock.Object, configMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.GenerateJwtToken(user));
        Assert.Contains("JWT Audience não configurado", exception.Message);
    }

    #endregion
}
