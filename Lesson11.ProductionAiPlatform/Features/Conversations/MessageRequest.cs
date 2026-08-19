using System.ComponentModel.DataAnnotations;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public sealed class MessageRequest : IValidatableObject
{
	public Guid? ConversationId { get; init; }

	[Required(ErrorMessage = "Content is required.")]
	public required string Content { get; init; }

	public string? Provider { get; init; }

	[Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
	public float? Temperature { get; init; }

	[Range(1, int.MaxValue, ErrorMessage = "MaxTokens must be greater than 0.")]
	public int? MaxTokens { get; init; }

	public bool AllowWriteProposal { get; init; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (!ConversationId.HasValue)
		{
			yield break;
		}

		if (Provider is not null)
		{
			yield return new ValidationResult(
				"Provider can only be supplied when starting a conversation.",
				[nameof(Provider)]);
		}

		if (Temperature is not null)
		{
			yield return new ValidationResult(
				"Temperature can only be supplied when starting a conversation.",
				[nameof(Temperature)]);
		}

		if (MaxTokens.HasValue)
		{
			yield return new ValidationResult(
				"MaxTokens can only be supplied when starting a conversation.",
				[nameof(MaxTokens)]);
		}
	}
}