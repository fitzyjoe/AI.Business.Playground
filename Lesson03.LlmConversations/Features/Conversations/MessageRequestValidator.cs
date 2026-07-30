namespace Lesson03.LlmConversations.Features.Conversations;

public class MessageRequestValidator
{
	public Dictionary<string, string[]>? Validate(MessageRequest request)
	{
		var errors = new Dictionary<string, string[]>();

		if (string.IsNullOrWhiteSpace(request.Content))
		{
			errors["content"] =
			[
				"Message content is required."
			];
		}

		if (request.ConversationId.HasValue)
		{
			if (request.SystemPrompt is not null)
			{
				errors["systemPrompt"] =
				[
					"SystemPrompt can only be supplied when starting a conversation."
				];
			}

			if (request.Model is not null)
			{
				errors["model"] =
				[
					"Model can only be supplied when starting a conversation."
				];
			}

			if (request.Temperature.HasValue)
			{
				errors["temperature"] =
				[
					"Temperature can only be supplied when starting a conversation."
				];
			}

			if (request.MaxTokens.HasValue)
			{
				errors["maxTokens"] =
				[
					"MaxTokens can only be supplied when starting a conversation."
				];
			}
		}

		return errors.Count == 0 ? null : errors;
	}
}