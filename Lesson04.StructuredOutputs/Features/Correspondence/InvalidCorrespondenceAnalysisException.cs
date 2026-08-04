namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed class InvalidCorrespondenceAnalysisException : Exception
{
	public InvalidCorrespondenceAnalysisException(string message) : base(message)
	{
		
	}

	public InvalidCorrespondenceAnalysisException(string message, Exception innerException) : base(message,
		innerException)
	{
		
	}
	
}