namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record AnalyzeCorrespondenceResponse(
	CorrespondenceAnalysis Analysis,
	WorkQueue Queue,
	string Model,
	TimeSpan Duration);