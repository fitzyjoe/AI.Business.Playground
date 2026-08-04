namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record ValueNoticeDetails
{
	public required int? TaxYear { get; init; }
	public required decimal? AssessedValue { get; init; }
	public required DateOnly? ProtestDeadline { get; init; }
	
	public void Validate()
	{
		if (TaxYear is < 1900 or > 3000)
		{
			throw new InvalidCorrespondenceAnalysisException(
				$"Invalid tax year '{TaxYear}'.");
		}

		if (AssessedValue < 0)
		{
			throw new InvalidCorrespondenceAnalysisException(
				$"Invalid assessed value '{AssessedValue}'.");
		}
	}
}