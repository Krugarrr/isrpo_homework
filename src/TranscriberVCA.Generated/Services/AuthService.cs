using System;
using System.Collections.Generic;
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
        _logger.LogInformation("Registration attempt for user {Username}", request.Username);

        if (_users.ContainsKey(request.Username))
        {
            _logger.LogWarning("Registration failed: user {Username} already exists", request.Username);
            throw new InvalidOperationException("User already exists");
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        _users[request.Username] = hashedPassword;

        var token = GenerateToken(request.Username);
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
        _logger.LogInformation("Login attempt for user {Username}", request.Username);

        if (!_users.ContainsKey(request.Username))
        {
            _logger.LogWarning("Login failed: user {Username} not found", request.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var storedHash = _users[request.Username];
        if (!BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
        {
            _logger.LogWarning("Login failed: invalid password for user {Username}", request.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = GenerateToken(request.Username);
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
            _logger.LogDebug("Token validated for user {Username}", username);
            return username;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return null;
        }
    }

    private string GenerateToken(string username)
    {
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