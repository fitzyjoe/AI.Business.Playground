namespace Lesson01.BasicPrompting.Features.Models.Execute;

public static class Endpoint
{
	public static IEndpointRouteBuilder MapExecutePrompt(this IEndpointRouteBuilder app)
	{
		app.MapPost("/api/prompt",
				async (
					AiRequest aiRequest,
					Handler handler,
					CancellationToken cancellationToken) =>
				{
					var response = await handler.Handle(aiRequest, cancellationToken);
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