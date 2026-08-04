using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public static class StructuredOutputJson
{
	public static JsonSerializerOptions Options { get; } = CreateOptions();

	private static JsonSerializerOptions CreateOptions()
	{
		var options =
			new JsonSerializerOptions(JsonSerializerDefaults.Web)
			{
				TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
				
				NumberHandling = JsonNumberHandling.Strict,
				
				PropertyNameCaseInsensitive = false,

				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

				RespectNullableAnnotations = true,

				RespectRequiredConstructorParameters = true
			};

		options.Converters.Add(
			new JsonStringEnumConverter(
				JsonNamingPolicy.CamelCase,
				allowIntegerValues: false));

		return options;
	}
}