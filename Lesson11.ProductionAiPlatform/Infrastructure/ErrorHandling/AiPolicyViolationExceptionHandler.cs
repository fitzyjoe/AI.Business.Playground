using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.AspNetCore.Diagnostics;

namespace Lesson11.ProductionAiPlatform.Infrastructure.ErrorHandling;

public sealed class AiPolicyViolationExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		if (exception is not AiPolicyViolationException policyViolation)
		{
			return false;
		}

		httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

		await Results.Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "AI request policy violation",
				detail: policyViolation.Message)
			.ExecuteAsync(httpContext);

		return true;
	}
}
