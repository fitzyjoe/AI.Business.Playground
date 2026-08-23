using System.ComponentModel.DataAnnotations;

namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record AnalyzeCorrespondenceRequest
{
	[Required]
	public required string DocumentText { get; init; }

	public string Provider { get; init; } = "ollama";
}
