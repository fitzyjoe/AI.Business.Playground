namespace Lesson05.McpFundamentals.Features.Properties;

public sealed record PropertySearchResult( int Count, IReadOnlyList<PropertyRecord> Properties);