using System.ComponentModel.DataAnnotations;

namespace Lesson03.LlmConversations.Features.Conversations;

public sealed class MessageRequest : IValidatableObject
{
	public Guid? ConversationId { get; init; }
	
	[Required(ErrorMessage = "Content is required.")]
	public required string Content { get; init; }

	public string? SystemPrompt { get; init; }

	public float? Temperature { get; init; }

	public string Provider { get; init; } = "ollama";
	
	public string? Model { get; init; }

	public int? MaxTokens { get; init; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (ConversationId.HasValue)
		{
			if (SystemPrompt is not null)
			{
				yield return new ValidationResult("SystemPrompt can only be supplied when starting a conversation.", [nameof(SystemPrompt)]);
			}

			if (Model is not null)
			{
				yield return new ValidationResult("Model can only be supplied when starting a conversation.", [nameof(Temperature)]);
			}

			if (MaxTokens.HasValue)
			{
				yield return new ValidationResult("MaxTokens can only be supplied when starting a conversation.", [nameof(MaxTokens)]);
			}
		}
	}
}