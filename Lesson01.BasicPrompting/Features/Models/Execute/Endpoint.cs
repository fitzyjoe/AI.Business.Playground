using Lesson01.Chat.Features.Models.Execute;

namespace Lesson01.Chat.Features.Models.Execute;

public static class Endpoint
{
	public static IEndpointRouteBuilder MapExecutePrompt(this IEndpointRouteBuilder app)
	{
		app.MapPost("/api/prompt",
				async (
					Request request,
					Handler handler,
					CancellationToken cancellationToken) =>
				{
					var response = await handler.Handle(request, cancellationToken);
					return Results.Ok(response);
				})
			.WithName("ExecutePromptPost")
			.WithSummary("Executes an AI prompt via POST")
			.WithDescription("Sends a prompt to the configured AI provider.")
			.Produces<AiResponse>(StatusCodes.Status200OK)
			.ProducesProblem(StatusCodes.Status500InternalServerError);

		return app;
	}
}