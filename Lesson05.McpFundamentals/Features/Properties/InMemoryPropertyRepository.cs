using System.Text.Json;

namespace Lesson05.McpFundamentals.Features.Properties;

public sealed class InMemoryPropertyRepository : IPropertyRepository
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly IReadOnlyList<PropertyRecord> _properties;

	public InMemoryPropertyRepository()
	{
		var dataFilePath = Path.Combine(AppContext.BaseDirectory, "Features", "Properties", "properties.json");

		if (!File.Exists(dataFilePath))
		{
			throw new FileNotFoundException("Property data file was not found.", dataFilePath);
		}

		using var stream = File.OpenRead(dataFilePath);
		var properties = JsonSerializer.Deserialize<PropertyRecord[]>(stream, JsonOptions)
			?? throw new InvalidOperationException("Property data file did not contain a valid property array.");

		if (properties.Length == 0)
		{
			throw new InvalidOperationException("Property data file must contain at least one property record.");
		}

		_properties = properties;
	}

	public Task<PropertyRecord?> GetByParcelNumberAsync(string parcelNumber,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var property = _properties.FirstOrDefault(property =>
			string.Equals(property.ParcelNumber, parcelNumber.Trim(), StringComparison.OrdinalIgnoreCase));
		return Task.FromResult(property);
	}

	public Task<IReadOnlyList<PropertyRecord>> SearchByOwnerAsync(string ownerName, int maxResults,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IReadOnlyList<PropertyRecord> matches = _properties
			.Where(property => property.OwnerName.Contains(ownerName.Trim(), StringComparison.OrdinalIgnoreCase))
			.Take(maxResults)
			.ToArray();
		return Task.FromResult(matches);
	}
}
