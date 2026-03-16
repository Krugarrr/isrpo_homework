using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TranscriberVCA.Generated.Models;

namespace TranscriberVCA.Services;

public class AuthService : IAuthService
{
    private readonly string _jwtSecret = "your-super-secret-key-min-32-characters-long!!!";
    private readonly Dictionary<string, string> _users = new(); // username -> hashed password

    public AuthResponse Register(RegisterRequest request)
    {
        if (_users.ContainsKey(request.Username))
            throw new InvalidOperationException("User already exists");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
        _users[request.Username] = hashedPassword;

        var token = GenerateToken(request.Username);
        return new AuthResponse { Token = token, Username = request.Username };
    }

    public AuthResponse Login(LoginRequest request)
    {
        if (!_users.ContainsKey(request.Username))
            throw new UnauthorizedAccessException("Invalid credentials");

        var storedHash = _users[request.Username];
        if (!BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = GenerateToken(request.Username);
        return new AuthResponse { Token = token, Username = request.Username };
    }

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
            return jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
        }
        catch
        {
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