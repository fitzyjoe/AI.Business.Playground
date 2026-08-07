using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Lesson05.McpFundamentals.Features.Properties;


[McpServerToolType]
public sealed class PropertyTools(IPropertyRepository _propertyRepository)
{
    [McpServerTool(
        Name = "lookup_property_by_parcel",
        Title = "Lookup Property by Parcel",
        ReadOnly = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Looks up one commercial property using an exact parcel number.")]
    public async Task<PropertyLookupResult>
        LookupPropertyByParcelAsync(
            [Description(
                "The exact parcel number, including punctuation. " +
                "Example: 0304-12-0042.")]
            string parcelNumber,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parcelNumber))
        {
            throw new McpProtocolException(
                "parcelNumber is required.",
                McpErrorCode.InvalidParams);
        }

        var property =
            await _propertyRepository.GetByParcelNumberAsync(
                parcelNumber,
                cancellationToken);

        if (property is null)
        {
            return new PropertyLookupResult(
                Found: false,
                Property: null);
        }

        return new PropertyLookupResult(
            Found: true,
            Property: property);
    }

    [McpServerTool(
        Name = "search_properties_by_owner",
        Title = "Search Properties by Owner",
        ReadOnly = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description(
        "Searches commercial properties using all or part " +
        "of the property owner's name.")]
    public async Task<PropertySearchResult> SearchPropertiesByOwnerAsync(
            [Description("All or part of the property owner's legal name.")]
            string ownerName,

            [Description("Maximum number of properties to return, from 1 to 25.")]
            int maxResults = 10,

            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new McpProtocolException(
                "ownerName is required.",
                McpErrorCode.InvalidParams);
        }

        if (maxResults is < 1 or > 25)
        {
            throw new McpProtocolException(
                "maxResults must be between 1 and 25.",
                McpErrorCode.InvalidParams);
        }
        
        // artificial delay to test cancellations
        await Task.Delay( TimeSpan.FromSeconds(5), cancellationToken);

        var properties =
            await _propertyRepository.SearchByOwnerAsync(
                ownerName,
                maxResults,
                cancellationToken);

        return new PropertySearchResult(
            properties.Count,
            properties);
    }
}