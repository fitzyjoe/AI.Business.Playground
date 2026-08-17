using Lesson09.Agents.Infrastructure.Ai;
using Microsoft.AspNetCore.Diagnostics;

namespace Lesson09.Agents.Infrastructure.ErrorHandling;

public sealed class UnsupportedAiProviderExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		if (exception is not UnsupportedAiProviderException unsupportedProvider)
		{
			return false;
		}

		httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

		await Results.Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "Unsupported AI provider",
				detail: unsupportedProvider.Message,
				extensions: new Dictionary<string, object?>
				{
					["provider"] = unsupportedProvider.Provider
				})
			.ExecuteAsync(httpContext);

		return true;
	}
}