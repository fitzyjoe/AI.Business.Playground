namespace Lesson05.McpFundamentals.Features.Properties;

public sealed class InMemoryPropertyRepository : IPropertyRepository
{
	private static readonly IReadOnlyList<PropertyRecord> Properties =
	[
		new PropertyRecord(ParcelNumber: "0304-12-0042", OwnerName: "ABC Commercial Holdings LLC",
			PropertyAddress: "1200 Main Street, McLean, VA 22101", Jurisdiction: "Fairfax County, Virginia",
			TaxYear: 2026, AssessedValue: 8_450_000m),
		new PropertyRecord(ParcelNumber: "0304-12-0043", OwnerName: "ABC Commercial Holdings LLC",
			PropertyAddress: "1210 Main Street, McLean, VA 22101", Jurisdiction: "Fairfax County, Virginia",
			TaxYear: 2026, AssessedValue: 5_275_000m),
		new PropertyRecord(ParcelNumber: "0752-03-0188", OwnerName: "Potomac Office Partners LP",
			PropertyAddress: "8500 Greensboro Drive, McLean, VA 22102", Jurisdiction: "Fairfax County, Virginia",
			TaxYear: 2026, AssessedValue: 21_900_000m)
	];

	public Task<PropertyRecord?> GetByParcelNumberAsync(string parcelNumber,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var property = Properties.FirstOrDefault(property =>
			string.Equals(property.ParcelNumber, parcelNumber.Trim(), StringComparison.OrdinalIgnoreCase));
		return Task.FromResult(property);
	}

	public Task<IReadOnlyList<PropertyRecord>> SearchByOwnerAsync(string ownerName, int maxResults,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IReadOnlyList<PropertyRecord> matches = Properties
			.Where(property => property.OwnerName.Contains(ownerName.Trim(), StringComparison.OrdinalIgnoreCase))
			.Take(maxResults).ToArray();
		return Task.FromResult(matches);
	}
}