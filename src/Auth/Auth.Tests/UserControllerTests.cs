using Auth.API.Controllers;
using Auth.API.Domain.DTOs;
using Auth.API.Domain.Entities;
using Auth.API.Domain.Enums;
using Auth.API.Domain.Interfaces;
using Auth.API.Domain.ModelViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Auth.Tests;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly UserController _userController;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserController>>();
        _userController = new UserController(_loggerMock.Object, _userServiceMock.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ShouldReturnBadRequest_WhenPageIsInvalid()
    {
        // Act
        var result = await _userController.GetAllUsers(page: 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O número da página deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnBadRequest_WhenPageSizeIsInvalid()
    {
        // Act
        var result = await _userController.GetAllUsers(pageSize: 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O tamanho da página deve ser maior que zero.", errors.Messages);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnBadRequest_WhenSortByIsInvalid()
    {
        // Act
        var result = await _userController.GetAllUsers(sortBy: "invalidField");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O campo sortBy deve ser 'email' ou vazio.", errors.Messages);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnOk_WhenParametersAreValid()
    {
        // Arrange
        var users = new List<User> 
        { 
            new User { Id = 1, Email = "test@example.com", Role = Roles.Admin } 
        };
        _userServiceMock.Setup(s => s.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Roles?>(), It.IsAny<bool>()))
            .ReturnsAsync(users);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<UserModelView>>(okResult.Value);
        Assert.Single(modelViews);
        Assert.Equal(users[0].Id, modelViews[0].Id);
        Assert.Equal(users[0].Email, modelViews[0].Email);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetAllUsersAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Roles?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelViews = Assert.IsType<List<UserModelView>>(okResult.Value);
        Assert.Empty(modelViews);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userController.GetUserById(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuário não encontrado.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Role = Roles.Admin };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _userController.GetUserById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<UserModelView>(okResult.Value);
        Assert.Equal(user.Id, modelView.Id);
        Assert.Equal(user.Email, modelView.Email);
        Assert.Equal(user.Role, modelView.Role);
    }

    #endregion

    #region AddUser Tests

    [Fact]
    public async Task AddUser_ShouldReturnBadRequest_WhenEmailIsEmpty()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "", Password = "password123", Role = Roles.Admin };

        // Act
        var result = await _userController.AddUser(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O email é obrigatório e não pode estar vazio.", errors.Messages);
    }

    [Fact]
    public async Task AddUser_ShouldReturnBadRequest_WhenEmailIsTooShort()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "a@b.com", Password = "password123", Role = Roles.Admin };

        // Act
        var result = await _userController.AddUser(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O email deve ter no mínimo 12 caracteres.", errors.Messages);
    }

    [Fact]
    public async Task AddUser_ShouldReturnBadRequest_WhenEmailFormatIsInvalid()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "invalidemail", Password = "password123", Role = Roles.Admin };

        // Act
        var result = await _userController.AddUser(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O email deve ter o formato válido: usuario@dominio.tld", errors.Messages);
    }

    [Fact]
    public async Task AddUser_ShouldReturnBadRequest_WhenPasswordIsEmpty()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "test@example.com", Password = "", Role = Roles.Admin };

        // Act
        var result = await _userController.AddUser(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("A senha (password) não pode ser vazia.", errors.Messages);
    }

    [Fact]
    public async Task AddUser_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "test@example.com", Password = "1234567", Role = Roles.Admin };

        // Act
        var result = await _userController.AddUser(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("A senha (password) deve ter no mínimo 8 caracteres.", errors.Messages);
    }

    [Fact]
    public async Task AddUser_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var validDto = new UserDTO { Email = "existing@example.com", Password = "password123", Role = Roles.Admin };
        var existingUser = new User { Id = 1, Email = "existing@example.com" };
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userController.AddUser(validDto);

        // Assert
        var conflictResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Equal("O email solicitado já está cadastrado.", conflictResult.Value);
    }

    [Fact]
    public async Task AddUser_ShouldReturnCreated_WhenServiceSucceeds()
    {
        // Arrange
        var validDto = new UserDTO { Email = "newuser@example.com", Password = "password123", Role = Roles.Admin };
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);
        _userServiceMock.Setup(s => s.AddUserAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userController.AddUser(validDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(UserController.GetUserById), createdResult.ActionName);
        var modelView = Assert.IsType<UserModelView>(createdResult.Value);
        Assert.Equal(validDto.Email, modelView.Email);
        Assert.Equal(validDto.Role, modelView.Role);
    }

    [Fact]
    public async Task AddUser_ShouldReturnInternalServerError_WhenServiceFails()
    {
        // Arrange
        var validDto = new UserDTO { Email = "newuser@example.com", Password = "password123", Role = Roles.Admin };
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);
        _userServiceMock.Setup(s => s.AddUserAsync(It.IsAny<User>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userController.AddUser(validDto);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        Assert.Equal("Ocorreu um erro ao tentar cadastrar o usuário no banco de dados.", serverErrorResult.Value);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequest_WhenDtoIsInvalid()
    {
        // Arrange
        var invalidDto = new UserDTO { Email = "", Password = "pass", Role = Roles.Admin };

        // Act
        var result = await _userController.UpdateUser(1, invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.NotEmpty(errors.Messages);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var validDto = new UserDTO { Email = "test@example.com", Password = "password123", Role = Roles.Admin };
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userController.UpdateUser(1, validDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuário não encontrado.", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnConflict_WhenEmailExistsForDifferentUser()
    {
        // Arrange
        var validDto = new UserDTO { Email = "existing@example.com", Password = "password123", Role = Roles.Admin };
        var existingUser = new User { Id = 2, Email = "existing@example.com" };
        var currentUser = new User { Id = 1, Email = "old@example.com" };
        
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(currentUser);

        // Act
        var result = await _userController.UpdateUser(1, validDto);

        // Assert
        var conflictResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
        Assert.Equal("O email solicitado já está cadastrado.", conflictResult.Value);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenServiceSucceeds()
    {
        // Arrange
        var validDto = new UserDTO { Email = "updated@example.com", Password = "password123", Role = Roles.Stock };
        var existingUser = new User { Id = 1, Email = "old@example.com", Role = Roles.Admin };
        
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("updated@example.com"))
            .ReturnsAsync((User)null);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userController.UpdateUser(1, validDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<UserModelView>(okResult.Value);
        Assert.Equal(validDto.Email, modelView.Email);
        Assert.Equal(validDto.Role, modelView.Role);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOk_WhenEmailBelongsToSameUser()
    {
        // Arrange
        var validDto = new UserDTO { Email = "user@example.com", Password = "password123", Role = Roles.Stock };
        var existingUser = new User { Id = 1, Email = "user@example.com", Role = Roles.Admin };
        
        _userServiceMock.Setup(s => s.GetUserByEmailAsync("user@example.com"))
            .ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userController.UpdateUser(1, validDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<UserModelView>(okResult.Value);
        Assert.Equal(validDto.Email, modelView.Email);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnInternalServerError_WhenServiceFails()
    {
        // Arrange
        var validDto = new UserDTO { Email = "updated@example.com", Password = "password123", Role = Roles.Stock };
        var existingUser = new User { Id = 1, Email = "old@example.com" };
        
        _userServiceMock.Setup(s => s.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(existingUser);
        _userServiceMock.Setup(s => s.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userController.UpdateUser(1, validDto);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        Assert.Equal("Ocorreu um erro ao tentar atualizar o usuário no banco de dados.", serverErrorResult.Value);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetUserByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userController.DeleteUser(1);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Usuário não encontrado.", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenServiceSucceeds()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.DeleteUserAsync(user))
            .ReturnsAsync(true);

        // Act
        var result = await _userController.DeleteUser(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnInternalServerError_WhenServiceFails()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.DeleteUserAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _userController.DeleteUser(1);

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        Assert.Equal("Ocorreu um erro ao tentar excluir o usuário no banco de dados.", serverErrorResult.Value);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenEmailIsEmpty()
    {
        // Arrange
        var invalidDto = new LoginDTO { Email = "", Password = "password123" };

        // Act
        var result = await _userController.Login(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("O email é obrigatório e não pode estar vazio.", errors.Messages);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenPasswordIsEmpty()
    {
        // Arrange
        var invalidDto = new LoginDTO { Email = "test@example.com", Password = "" };

        // Act
        var result = await _userController.Login(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<ValidationErrors>(badRequestResult.Value);
        Assert.Contains("A senha (password) não pode ser vazia.", errors.Messages);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var validDto = new LoginDTO { Email = "test@example.com", Password = "password123" };
        _userServiceMock.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _userController.Login(validDto);

        // Assert
        var unauthorizedResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        Assert.Equal("Email ou senha (password) inválidos. Tente novamente.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithToken_WhenCredentialsAreValid()
    {
        // Arrange
        var validDto = new LoginDTO { Email = "admin@example.com", Password = "password123" };
        var user = new User { Id = 1, Email = "admin@example.com", Role = Roles.Admin };
        var token = "sample.jwt.token";
        
        _userServiceMock.Setup(s => s.Login("admin@example.com", "password123"))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.GenerateJwtToken(user))
            .Returns(token);

        // Act
        var result = await _userController.Login(validDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var modelView = Assert.IsType<LoggedUserModelView>(okResult.Value);
        Assert.Equal(user.Email, modelView.Email);
        Assert.Equal(user.Role, modelView.Role);
        Assert.Equal(token, modelView.AccessToken);
    }

    [Fact]
    public async Task Login_ShouldCallGenerateJwtToken_WhenLoginSucceeds()
    {
        // Arrange
        var validDto = new LoginDTO { Email = "admin@example.com", Password = "password123" };
        var user = new User { Id = 1, Email = "admin@example.com", Role = Roles.Admin };
        
        _userServiceMock.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.GenerateJwtToken(user))
            .Returns("token");

        // Act
        await _userController.Login(validDto);

        // Assert
        _userServiceMock.Verify(s => s.GenerateJwtToken(user), Times.Once);
    }

    #endregion
}
