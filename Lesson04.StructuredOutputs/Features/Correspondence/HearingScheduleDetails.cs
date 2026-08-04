using System.Globalization;

namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed record HearingScheduleDetails
{
	public required DateOnly? HearingDate { get; init; }

	// this would be interpreted later to match to a time zone if one was not specified
	public required string? HearingTime { get; init; }

	public required string? Location { get; init; }
	
	public void Validate()
	{
		if (HearingTime is null)
		{
			return;
		}

		if (!TimeOnly.TryParse(
			    HearingTime,
			    CultureInfo.GetCultureInfo("en-US"),
			    DateTimeStyles.AllowWhiteSpaces,
			    out _))
		{
			throw new InvalidCorrespondenceAnalysisException(
				$"Invalid hearing time '{HearingTime}'.");
		}
	}
}