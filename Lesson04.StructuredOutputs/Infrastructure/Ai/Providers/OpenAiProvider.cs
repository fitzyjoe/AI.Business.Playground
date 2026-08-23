using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai.Providers;

public sealed class OpenAiProvider(IOptions<OpenAiOptions> options) : IAiProvider
{
	private readonly OpenAiOptions _options = options.Value;

	public async Task<AiChatResponse> SendAsync(
		AiChatRequest aiRequest,
		CancellationToken cancellationToken = default)
	{
		var model = aiRequest.Model ?? _options.Model;
		var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
		             ?? throw new InvalidOperationException(
			             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

		var messages = aiRequest.Messages.Select(ToOpenAiMessage).ToList();
		var completionOptions = new ChatCompletionOptions
		{
			Temperature = aiRequest.Temperature,
			MaxOutputTokenCount = aiRequest.MaxTokens
		};

		if (aiRequest.ResponseFormat is not null)
		{
			completionOptions.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
				jsonSchemaFormatName: "correspondence_analysis",
				jsonSchema: BinaryData.FromString(aiRequest.ResponseFormat.ToJsonString()),
				jsonSchemaIsStrict: true);
		}

		var chatClient = new ChatClient(model, apiKey);
		var stopwatch = Stopwatch.StartNew();
		ChatCompletion completion = await chatClient.CompleteChatAsync(
			messages,
			completionOptions,
			cancellationToken);
		stopwatch.Stop();

		var text = string.Concat(completion.Content.Select(part => part.Text));
		return new AiChatResponse(text, model, stopwatch.Elapsed);
	}

	private static ChatMessage ToOpenAiMessage(AiChatMessage message)
	{
		return message.Role switch
		{
			AiMessageRole.System => new SystemChatMessage(message.Content),
			AiMessageRole.User => new UserChatMessage(message.Content),
			AiMessageRole.Assistant => new AssistantChatMessage(message.Content),
			_ => throw new ArgumentOutOfRangeException(
				nameof(message.Role),
				message.Role,
				"Unsupported AI message role.")
		};
	}
}
