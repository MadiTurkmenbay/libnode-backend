using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LibNode.Api.Authentication;

public class TranslatorApiKeyAuthenticationHandler : AuthenticationHandler<TranslatorApiKeyAuthenticationOptions>
{
    public TranslatorApiKeyAuthenticationHandler(
        IOptionsMonitor<TranslatorApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Translator API key is not configured."));
        }

        var providedKey = Request.Headers["x-api-key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();
            if (authorizationHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                providedKey = authorizationHeader["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!FixedTimeEquals(providedKey, Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid translator API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "translator-integration"),
            new Claim(ClaimTypes.Name, "translator-integration"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, TranslatorApiKeyAuthenticationDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TranslatorApiKeyAuthenticationDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
