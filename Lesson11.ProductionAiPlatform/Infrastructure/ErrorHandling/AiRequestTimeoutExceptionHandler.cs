using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.AspNetCore.Diagnostics;

namespace Lesson11.ProductionAiPlatform.Infrastructure.ErrorHandling;

public sealed class AiRequestTimeoutExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		if (exception is not AiRequestTimeoutException timeout)
		{
			return false;
		}

		httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

		await Results.Problem(
				statusCode: StatusCodes.Status504GatewayTimeout,
				title: "AI request timed out",
				detail: timeout.Message)
			.ExecuteAsync(httpContext);

		return true;
	}
}
