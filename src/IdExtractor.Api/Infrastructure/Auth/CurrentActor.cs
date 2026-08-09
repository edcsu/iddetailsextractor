using System.Security.Claims;
using IdExtractor.SharedKernel.Auditing;

namespace IdExtractor.Api.Infrastructure.Auth;

/// <summary>
/// Reads the authenticated API-key identity off the current request so the audit
/// interceptor can stamp who made a change.
/// </summary>
public sealed class CurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public string? ActorId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
