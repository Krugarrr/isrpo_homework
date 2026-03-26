using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TranscriberVCA.Generated.Models;
using TranscriberVCA.Generated.Services;
using Xunit;

namespace TranscriberVCA.Tests;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var logger = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(logger.Object);
    }

    [Fact]
    public void Register_NewUser_ReturnsTokenAndUsername()
    {
        var request = new RegisterRequest { Username = "sanechek", Password = "password123" };

        var result = _authService.Register(request);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("sanechek");
    }

    [Fact]
    public void Register_DuplicateUser_ThrowsException()
    {
        var request = new RegisterRequest { Username = "gigachad", Password = "password123" };
        _authService.Register(request);

        var act = () => _authService.Register(request);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User already exists");
    }

    [Fact]
    public void Login_ValidCredentials_ReturnsToken()
    {
        var registerRequest = new RegisterRequest { Username = "eminchik", Password = "password123" };
        _authService.Register(registerRequest);

        var loginRequest = new LoginRequest { Username = "eminchik", Password = "password123" };
        var result = _authService.Login(loginRequest);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("eminchik");
    }

    [Fact]
    public void Login_WrongPassword_ThrowsUnauthorized()
    {
        var registerRequest = new RegisterRequest { Username = "maximche", Password = "correct" };
        _authService.Register(registerRequest);

        var loginRequest = new LoginRequest { Username = "maximche", Password = "incorrect" };
        var act = () => _authService.Login(loginRequest);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public void Login_NonExistentUser_ThrowsUnauthorized()
    {
        var loginRequest = new LoginRequest { Username = "skibidist", Password = "password" };
        var act = () => _authService.Login(loginRequest);

        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsUsername()
    {
        var registerRequest = new RegisterRequest { Username = "kilagen", Password = "password123" };
        var registerResult = _authService.Register(registerRequest);

        var username = _authService.ValidateToken(registerResult.Token);

        username.Should().Be("kilagen");
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var result = _authService.ValidateToken("invalid.token.here");

        result.Should().BeNull();
    }
}