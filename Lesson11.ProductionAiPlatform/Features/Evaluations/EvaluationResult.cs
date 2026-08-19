namespace Lesson11.ProductionAiPlatform.Features.Evaluations;

public sealed record EvaluationCaseResult(
	string Name,
	bool Passed,
	string Detail,
	string Response);

public sealed record EvaluationRunResult(
	int Passed,
	int Failed,
	EvaluationCaseResult[] Cases);