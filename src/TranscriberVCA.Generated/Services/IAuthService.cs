using TranscriberVCA.Generated.Models;

namespace TranscriberVCA.Services;

public interface IAuthService
{
    AuthResponse Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
    string? ValidateToken(string token);
}