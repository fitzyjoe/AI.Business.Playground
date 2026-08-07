namespace Lesson05.McpFundamentals.Features.Properties;

public sealed record PropertyRecord(
	string ParcelNumber,
	string OwnerName,
	string PropertyAddress,
	string Jurisdiction,
	int TaxYear,
	decimal AssessedValue);