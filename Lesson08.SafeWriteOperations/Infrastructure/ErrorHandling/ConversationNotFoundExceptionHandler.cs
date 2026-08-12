using Lesson08.SafeWriteOperations.Features.Conversations;
using Microsoft.AspNetCore.Diagnostics;

namespace Lesson08.SafeWriteOperations.Infrastructure.ErrorHandling;

public sealed class ConversationNotFoundExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		if (exception is not ConversationNotFoundException notFound)
		{
			return false;
		}

		httpContext.Response.StatusCode =
			StatusCodes.Status404NotFound;

		await Results.Problem(
				statusCode: StatusCodes.Status404NotFound,
				title: "Conversation not found",
				detail: notFound.Message,
				extensions: new Dictionary<string, object?>
				{
					["conversationId"] = notFound.ConversationId
				})
			.ExecuteAsync(httpContext);

		return true;
	}
}