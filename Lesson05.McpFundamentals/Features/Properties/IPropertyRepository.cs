namespace Lesson05.McpFundamentals.Features.Properties;

public interface IPropertyRepository
{
	Task<PropertyRecord?> GetByParcelNumberAsync(string parcelNumber, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<PropertyRecord>> SearchByOwnerAsync(string ownerName, int maxResults, CancellationToken cancellationToken = default);
}