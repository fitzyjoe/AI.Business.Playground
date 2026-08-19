namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public static class AiCapabilities
{
	public const string ProposePropertyReview = "property-review:propose";
}

public sealed record AiExecutionContext(
	string Caller,
	string CorrelationId,
	IReadOnlySet<string> Capabilities)
{
	public bool HasCapability(string capability)
	{
		return Capabilities.Contains(capability);
	}
}

public sealed class AiExecutionContextAccessor
{
	private static readonly AsyncLocal<AiExecutionContext?> CurrentContext = new();

	public AiExecutionContext? Current => CurrentContext.Value;

	public IDisposable Push(AiExecutionContext context)
	{
		var previous = CurrentContext.Value;
		CurrentContext.Value = context;

		return new RestoreScope(previous);
	}

	private sealed class RestoreScope(AiExecutionContext? previous) : IDisposable
	{
		public void Dispose()
		{
			CurrentContext.Value = previous;
		}
	}
}