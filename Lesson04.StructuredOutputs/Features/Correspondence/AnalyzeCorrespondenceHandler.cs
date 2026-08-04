using System.Text.Json;
using System.Text.Json.Schema;
using Lesson04.StructuredOutputs.Infrastructure.Ai;

namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed class AnalyzeCorrespondenceHandler(
	IAiProviderFactory _aiProviderFactory)
{
	private const string ProviderName = "ollama";

	public async Task<AnalyzeCorrespondenceResponse> HandleAsync(AnalyzeCorrespondenceRequest request,
		CancellationToken cancellationToken)
	{
		var schema = StructuredOutputJson.Options.GetJsonSchemaAsNode(typeof(CorrespondenceAnalysis));
		var aiRequest = new AiChatRequest
		{
			Messages =
			[
				new AiChatMessage
				{
					Role = AiMessageRole.System, 
					Content = BuildSystemPrompt()
				},
				new AiChatMessage
				{
					Role = AiMessageRole.User,
					Content = BuildUserPrompt(request.DocumentText, schema.ToJsonString())
				}
			],

			// Extraction should be consistent rather than creative.
			Temperature = 0, 
			ResponseFormat = schema,

			// Wait for one complete JSON object.
			Stream = false
		};
		var provider = _aiProviderFactory.GetProvider(ProviderName);
		var aiResponse = await provider.SendAsync(aiRequest, cancellationToken);
		var analysis = Deserialize(aiResponse.Text).NormalizeAndValidate();
		return new AnalyzeCorrespondenceResponse(analysis, aiResponse.Model, aiResponse.Duration);
	}

	private static CorrespondenceAnalysis Deserialize(string json)
	{
		try
		{
			return JsonSerializer.Deserialize<CorrespondenceAnalysis>(json, StructuredOutputJson.Options) ??
			       throw new StructuredOutputException("The model returned an empty result.");
		}
		catch (JsonException exception)
		{
			throw new StructuredOutputException(
				"The model response could not be deserialized as correspondence analysis.", exception);
		}
	}

	private static string BuildSystemPrompt()
	{
		return """
		       You analyze OCR text produced from scanned
		       property-tax correspondence.

		       Every document is exactly one of these types:

		       1. HearingSchedule
		       2. ValueNotice

		       Extract only information explicitly supported by
		       the OCR text.

		       Use null when a value is missing, unreadable,
		       ambiguous, or uncertain.

		       Never invent customer names, parcel numbers,
		       addresses, dates, times, values, or deadlines.

		       Populate the detail object corresponding to the
		       selected document type.

		       Return only the requested structured result.
		       """;
	}

	private static string BuildUserPrompt(string documentText, string schema)
	{
		return $"""
		        Analyze the following OCR text.

		        --- BEGIN OCR TEXT ---

		        {documentText}

		        --- END OCR TEXT ---

		        Return an object matching this JSON schema:

		        {schema}
		        """;
	}
}