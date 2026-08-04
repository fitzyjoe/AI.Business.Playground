namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed class StructuredOutputException : Exception
{
	public StructuredOutputException(string message) : base(message)
	{
	}

	public StructuredOutputException(string message, Exception innerException) : base(message, innerException)
	{
	}
}