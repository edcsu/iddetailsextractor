using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IdExtractor.Api.Infrastructure.Auth;

/// <summary>
/// Authenticates requests carrying a valid <c>X-Api-Key</c> header against the hashed
/// keys in <see cref="ApiKeysOptions"/>. On success the caller's configured name
/// becomes the <see cref="ClaimTypes.NameIdentifier"/> claim, which
/// <see cref="CurrentActor"/> surfaces to the audit interceptor as the acting identity.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeysOptions> apiKeys)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var provided) ||
            string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {ApiKeyDefaults.HeaderName} header."));
        }

        var hashedKey = Hash(provided.ToString());
        var match = apiKeys.Value.Keys.Find(key => key.HashedKey == hashedKey);
        if (match is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, match.Name)],
            ApiKeyDefaults.AuthenticationScheme);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), ApiKeyDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
}
