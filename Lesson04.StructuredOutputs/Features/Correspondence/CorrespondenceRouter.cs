namespace Lesson04.StructuredOutputs.Features.Correspondence;

public static class CorrespondenceRouter
{
	public static WorkQueue Route(CorrespondenceType documentType)
	{
		return documentType switch
		{
			CorrespondenceType.HearingSchedule => WorkQueue.Hearings,
			CorrespondenceType.ValueNotice => WorkQueue.ValuationReview,
			_ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType,
				"Unsupported correspondence type.")
		};
	}
}