using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Authentication;

public sealed class DemoApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var suppliedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var readerKey = Environment.GetEnvironmentVariable("AI_DEMO_READER_KEY");
        var reviewerKey = Environment.GetEnvironmentVariable("AI_DEMO_REVIEWER_KEY");

        if (!string.IsNullOrWhiteSpace(readerKey) && string.Equals(suppliedKey, readerKey, StringComparison.Ordinal))
        {
            return Task.FromResult(CreateSuccess("reader-user", "reader@example.com", ["Reader"]));
        }

        if (!string.IsNullOrWhiteSpace(reviewerKey) && string.Equals(suppliedKey, reviewerKey, StringComparison.Ordinal))
        {
            return Task.FromResult(CreateSuccess("reviewer-user", "reviewer@example.com", ["Reader", "Reviewer"]));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
    }

    private AuthenticateResult CreateSuccess(string userId, string name, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.NameIdentifier, userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}