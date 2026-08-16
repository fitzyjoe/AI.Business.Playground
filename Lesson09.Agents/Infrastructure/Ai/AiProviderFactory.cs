namespace Lesson09.Agents.Infrastructure.Ai;

public sealed class AiProviderFactory : IAiProviderFactory
{
	private readonly IReadOnlyDictionary<string, IAiProvider> _providers;

	public AiProviderFactory(IEnumerable<IAiProvider> providers)
	{
		_providers = providers.ToDictionary(
			provider => provider.Name,
			StringComparer.OrdinalIgnoreCase);
	}

	public IAiProvider GetProvider(string provider)
	{
		if (_providers.TryGetValue(provider, out var aiProvider))
		{
			return aiProvider;
		}

		throw new NotSupportedException($"AI provider '{provider}' is not supported.");
	}
}