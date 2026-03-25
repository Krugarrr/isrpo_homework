using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TranscriberVCA.Generated.Models;
using TranscriberVCA.Services;

namespace TranscriberVCA.Generated.Services;

/// <summary>
/// AuthService
/// </summary>
public class AuthService : IAuthService
{
    private static readonly ActivitySource ActivitySource = new("TranscriberVCA.Auth");
    private readonly string _jwtSecret = "your-super-secret-key-min-32-characters-long!!!";
    private readonly Dictionary<string, string> _users = new();
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public AuthResponse Register(RegisterRequest request)
    {
        using var activity = ActivitySource.StartActivity("AuthService.Register");
        activity?.SetTag("auth.username", request.Username);

        _logger.LogInformation("Registration attempt for user {Username}", request.Username);

        if (_users.ContainsKey(request.Username))
        {
            activity?.SetTag("auth.result", "user_exists");
            activity?.SetStatus(ActivityStatusCode.Error, "User already exists");
            _logger.LogWarning("Registration failed: user {Username} already exists", request.Username);
            throw new InvalidOperationException("User already exists");
        }

        using (var hashActivity = ActivitySource.StartActivity("AuthService.HashPassword"))
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            _users[request.Username] = hashedPassword;
        }

        var token = GenerateToken(request.Username);
        activity?.SetTag("auth.result", "success");
        activity?.SetTag("auth.total_users", _users.Count);

        _logger.LogInformation("User {Username} registered successfully. Total users: {UserCount}",
            request.Username, _users.Count);
        return new AuthResponse { Token = token, Username = request.Username };
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public AuthResponse Login(LoginRequest request)
    {
        using var activity = ActivitySource.StartActivity("AuthService.Login");
        activity?.SetTag("auth.username", request.Username);

        _logger.LogInformation("Login attempt for user {Username}", request.Username);

        if (!_users.ContainsKey(request.Username))
        {
            activity?.SetTag("auth.result", "user_not_found");
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid credentials");
            _logger.LogWarning("Login failed: user {Username} not found", request.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        bool passwordValid;
        using (var verifyActivity = ActivitySource.StartActivity("AuthService.VerifyPassword"))
        {
            var storedHash = _users[request.Username];
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, storedHash);
        }

        if (!passwordValid)
        {
            activity?.SetTag("auth.result", "invalid_password");
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid credentials");
            _logger.LogWarning("Login failed: invalid password for user {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = GenerateToken(request.Username);
        activity?.SetTag("auth.result", "success");

        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        return new AuthResponse { Token = token, Username = request.Username };
    }

    /// <summary>
    /// ValidateToken
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public string? ValidateToken(string token)
    {
        using var activity = ActivitySource.StartActivity("AuthService.ValidateToken");

        try
        {
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier).Value;

            activity?.SetTag("auth.username", username);
            activity?.SetTag("auth.token_valid", true);
            _logger.LogDebug("Token validated for user {Username}", username);
            return username;
        }
        catch (Exception ex)
        {
            activity?.SetTag("auth.token_valid", false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return null;
        }
    }

    private string GenerateToken(string username)
    {
        using var activity = ActivitySource.StartActivity("AuthService.GenerateToken");
        activity?.SetTag("auth.username", username);

        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, username) }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}