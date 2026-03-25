using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriberVCA.Services;

namespace TranscriberVCA.Generated;

/// <inheritdoc />
public class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthService _authService;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="encoder"></param>
    /// <param name="clock"></param>
    /// <param name="authService"></param>
    [Obsolete("Obsolete")]
    public BearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IAuthService authService)
        : base(options, logger, encoder, clock)
    {
        _authService = authService;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogInformation("=== Auth handler called ===");
        Logger.LogInformation("Headers: {0}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}")));

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            Logger.LogWarning("Missing Authorization header");
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        Logger.LogInformation("Auth header: {0}", authHeader);

        if (!authHeader.StartsWith("Bearer "))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));

        var token = authHeader.Substring("Bearer ".Length).Trim();
        Logger.LogInformation("Token: {0}", token);

        var userId = _authService.ValidateToken(token);
        Logger.LogInformation("UserId: {0}", userId ?? "NULL");

        if (userId == null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}