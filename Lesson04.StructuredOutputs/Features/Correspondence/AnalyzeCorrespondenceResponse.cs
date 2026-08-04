namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record AnalyzeCorrespondenceResponse(
	CorrespondenceAnalysis Analysis,
	string Model,
	TimeSpan Duration);