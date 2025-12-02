using Auth.API.Context;
using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auth.Tests;

public class UserRepositoryTests
{
    private UsersContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new UsersContext(options);
    }

    private async Task<UsersContext> CreateContextWithData()
    {
        var context = CreateContext();

        var users = new List<User>
        {
            new User { Id = 1, Email = "admin@example.com", Password = "password123", Role = Roles.Admin, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 2, Email = "sales@example.com", Password = "password456", Role = Roles.Sales, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 3, Email = "stock@example.com", Password = "password789", Role = Roles.Stock, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 4, Email = "another.admin@example.com", Password = "adminpass", Role = Roles.Admin, IsDeleted = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = 5, Email = "deleted@example.com", Password = "deletedpass", Role = Roles.Sales, IsDeleted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, DeletedAt = DateTime.UtcNow }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        return context;
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers_WhenNoFiltersApplied()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync();

        // Assert
        Assert.Equal(4, result.Count); // 4 usuários não deletados
        Assert.DoesNotContain(result, u => u.IsDeleted);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldFilterByEmail_WhenEmailProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync(email: "admin");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.Contains("admin", u.Email));
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldFilterByRole_WhenRoleProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync(role: Roles.Admin);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.Equal(Roles.Admin, u.Role));
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldSortByEmailAscending_WhenSortByEmailAndAscendingTrue()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync(sortBy: "email", ascending: true);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("admin@example.com", result[0].Email);
        Assert.Equal("another.admin@example.com", result[1].Email);
        Assert.Equal("sales@example.com", result[2].Email);
        Assert.Equal("stock@example.com", result[3].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldSortByEmailDescending_WhenSortByEmailAndAscendingFalse()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync(sortBy: "email", ascending: false);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("stock@example.com", result[0].Email);
        Assert.Equal("sales@example.com", result[1].Email);
        Assert.Equal("another.admin@example.com", result[2].Email);
        Assert.Equal("admin@example.com", result[3].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldSortByCreatedAtAscending_WhenDefaultSortApplied()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);

        var user1 = new User { Id = 1, Email = "user1@example.com", Password = "pass1", Role = Roles.Admin, CreatedAt = DateTime.UtcNow.AddDays(-3) };
        var user2 = new User { Id = 2, Email = "user2@example.com", Password = "pass2", Role = Roles.Sales, CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var user3 = new User { Id = 3, Email = "user3@example.com", Password = "pass3", Role = Roles.Stock, CreatedAt = DateTime.UtcNow.AddDays(-1) };

        await context.Users.AddRangeAsync(user1, user2, user3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllUsersAsync(ascending: true);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("user1@example.com", result[0].Email);
        Assert.Equal("user2@example.com", result[1].Email);
        Assert.Equal("user3@example.com", result[2].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldApplyPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var page1 = await repository.GetAllUsersAsync(page: 1, pageSize: 2);
        var page2 = await repository.GetAllUsersAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldExcludeDeletedUsers_Always()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetAllUsersAsync();

        // Assert
        Assert.DoesNotContain(result, u => u.IsDeleted);
        Assert.DoesNotContain(result, u => u.Email == "deleted@example.com");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("admin@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByIdAsync(5); // Usuário deletado

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByEmailAsync("admin@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin@example.com", result.Email);
        Assert.Equal(Roles.Admin, result.Role);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetUserByEmailAsync("deleted@example.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddUserAsync Tests

    [Fact]
    public async Task AddUserAsync_ShouldAddUser_Successfully()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User
        {
            Email = "newuser@example.com",
            Password = "newpassword",
            Role = Roles.Sales
        };

        // Act
        await repository.AddUserAsync(user);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Users.FindAsync(user.Id);
        Assert.NotNull(result);
        Assert.Equal("newuser@example.com", result.Email);
    }

    [Fact]
    public async Task AddUserAsync_ShouldSetCreatedAtAndUpdatedAt_WhenAdding()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var beforeAdd = DateTime.UtcNow;
        var user = new User
        {
            Email = "newuser@example.com",
            Password = "newpassword",
            Role = Roles.Sales
        };

        // Act
        await repository.AddUserAsync(user);
        await repository.SaveChangesAsync();
        var afterAdd = DateTime.UtcNow;

        // Assert
        var result = await context.Users.FindAsync(user.Id);
        Assert.NotNull(result);
        Assert.InRange(result.CreatedAt, beforeAdd, afterAdd);
        Assert.InRange(result.UpdatedAt, beforeAdd, afterAdd);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUser_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);
        var user = await repository.GetUserByIdAsync(1);
        user.Email = "updated@example.com";
        user.Role = Roles.Stock;

        // Act
        await repository.UpdateUserAsync(user);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Users.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("updated@example.com", result.Email);
        Assert.Equal(Roles.Stock, result.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateUpdatedAt_WhenUpdating()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);
        var user = await repository.GetUserByIdAsync(1);
        var originalUpdatedAt = user.UpdatedAt;
        
        // Aguarda um tempo para garantir que o UpdatedAt seja diferente
        await Task.Delay(10);
        
        user.Email = "updated@example.com";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await repository.UpdateUserAsync(user);
        await repository.SaveChangesAsync();
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var result = await context.Users.FindAsync(1);
        Assert.NotNull(result);
        Assert.InRange(result.UpdatedAt, beforeUpdate, afterUpdate);
        Assert.NotEqual(originalUpdatedAt, result.UpdatedAt);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ShouldMarkAsDeleted_Successfully()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);
        var user = await repository.GetUserByIdAsync(1);

        // Act
        await repository.DeleteUserAsync(user);
        await repository.SaveChangesAsync();

        // Assert
        var result = await context.Users.FindAsync(1);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldSetDeletedAt_WhenDeleting()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);
        var user = await repository.GetUserByIdAsync(1);
        var beforeDelete = DateTime.UtcNow;

        // Act
        await repository.DeleteUserAsync(user);
        await repository.SaveChangesAsync();
        var afterDelete = DateTime.UtcNow;

        // Assert
        var result = await context.Users.FindAsync(1);
        Assert.NotNull(result);
        Assert.NotNull(result.DeletedAt);
        Assert.InRange(result.DeletedAt.Value, beforeDelete, afterDelete);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.Login("admin@example.com", "password123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin@example.com", result.Email);
        Assert.Equal(Roles.Admin, result.Role);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenEmailIsInvalid()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.Login("invalid@example.com", "password123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.Login("admin@example.com", "wrongpassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.Login("deleted@example.com", "deletedpass");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenChangesAreSaved()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User
        {
            Email = "newuser@example.com",
            Password = "newpassword",
            Role = Roles.Sales
        };
        await repository.AddUserAsync(user);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenNoChangesToSave()
    {
        // Arrange
        await using var context = await CreateContextWithData();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.False(result);
    }

    #endregion
}
