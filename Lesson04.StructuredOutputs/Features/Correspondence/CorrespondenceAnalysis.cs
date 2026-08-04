namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record CorrespondenceAnalysis
{
	public required CorrespondenceType DocumentType { get; init; }

	public required string? CustomerName { get; init; }

	public required string? PropertyAddress { get; init; }

	public required string? ParcelNumber { get; init; }

	public required HearingScheduleDetails? HearingSchedule { get; init; }

	public required ValueNoticeDetails? ValueNotice { get; init; }
	
	public CorrespondenceAnalysis NormalizeAndValidate()
	{
		return DocumentType switch
		{
			CorrespondenceType.HearingSchedule =>
				NormalizeHearingSchedule(),

			CorrespondenceType.ValueNotice =>
				NormalizeValueNotice(),

			_ => throw new InvalidCorrespondenceAnalysisException(
				$"Unsupported correspondence type '{DocumentType}'.")
		};
	}
	
	private CorrespondenceAnalysis NormalizeHearingSchedule()
	{
		if (HearingSchedule is null)
		{
			throw new InvalidCorrespondenceAnalysisException(
				"The model classified the document as a hearing schedule but did not return hearing details.");
		}

		HearingSchedule.Validate();

		return this with
		{
			ValueNotice = null
		};
	}

	private CorrespondenceAnalysis NormalizeValueNotice()
	{
		if (ValueNotice is null)
		{
			throw new InvalidCorrespondenceAnalysisException(
				"The model classified the document as a value notice but did not return value notice details.");
		}

		ValueNotice.Validate();

		return this with
		{
			HearingSchedule = null
		};
	}
}