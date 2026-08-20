using System.Security.Claims;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Authentication;

public sealed class CurrentUser(IHttpContextAccessor _httpContextAccessor) : ICurrentUser
{
	private ClaimsPrincipal User =>
		_httpContextAccessor.HttpContext?.User
		?? throw new InvalidOperationException("There is no current HTTP user.");

	public string Id =>
		User.FindFirstValue(ClaimTypes.NameIdentifier)
		?? throw new InvalidOperationException("The authenticated user does not contain a name identifier.");

	public string Name =>
		User.Identity?.Name
		?? throw new InvalidOperationException("The authenticated user does not contain a name.");

	public bool IsInRole(string role)
	{
		return User.IsInRole(role);
	}
}