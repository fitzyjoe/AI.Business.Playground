using System.Text.Json;
using System.Text.Json.Schema;
using Lesson04.StructuredOutputs.Infrastructure.Ai;

namespace Lesson04.StructuredOutputs.Features.Correspondence;

public sealed class AnalyzeCorrespondenceHandler(IAiProviderFactory aiProviderFactory,
	ILogger<AnalyzeCorrespondenceHandler> logger)
{
	private const string ProviderName = "ollama";

	public async Task<AnalyzeCorrespondenceResponse> HandleAsync(AnalyzeCorrespondenceRequest request,
		CancellationToken cancellationToken)
	{
		var schema = StructuredOutputJson.Options.GetJsonSchemaAsNode(typeof(CorrespondenceAnalysis));
		var
			aiRequest = new AiChatRequest
			{
				Messages =
				[
					new AiChatMessage { Role = AiMessageRole.System, Content = BuildSystemPrompt() },
					new AiChatMessage
					{
						Role = AiMessageRole.User,
						Content = BuildUserPrompt(request.DocumentText, schema.ToJsonString())
					}
				],
				
				// Extraction should be consistent rather than creative.
				Temperature = 0, ResponseFormat = schema,

				// Wait for one complete JSON object.
				Stream = false
			};
		var provider = aiProviderFactory.GetProvider(ProviderName);
		var aiResponse = await provider.SendAsync(aiRequest, cancellationToken);
		logger.LogInformation(
			"Raw structured response: {Response}",
			aiResponse.Text);
		var analysis = Deserialize(aiResponse.Text).NormalizeAndValidate();
		var queue = CorrespondenceRouter.Route(analysis.DocumentType);
		return new AnalyzeCorrespondenceResponse(analysis, queue, aiResponse.Model, aiResponse.Duration);
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
	
	private static CorrespondenceAnalysis Normalize(CorrespondenceAnalysis analysis)
	{
		switch (analysis.DocumentType)
		{
			case CorrespondenceType.HearingSchedule:

				if (analysis.HearingSchedule is null)
				{
					throw new InvalidCorrespondenceAnalysisException(
						"The model classified the document as a hearing schedule " +
						"but did not return hearing details.");
				}

				return analysis with
				{
					ValueNotice = null
				};

			case CorrespondenceType.ValueNotice:

				if (analysis.ValueNotice is null)
				{
					throw new InvalidCorrespondenceAnalysisException(
						"The model classified the document as a value notice " +
						"but did not return value notice details.");
				}

				return analysis with
				{
					HearingSchedule = null
				};

			default:
				throw new InvalidCorrespondenceAnalysisException(
					$"Unsupported document type: {analysis.DocumentType}");
		}
	}

	private static string BuildSystemPrompt()
	{
		return
			"""
			 You analyze OCR text produced from scanned property-tax correspondence. Every document is exactly one of these types:
			
			1. HearingSchedule
			2. ValueNotice
			
			Extract only information explicitly supported by the OCR text. Use null when a value is missing, unreadable, ambiguous, or uncertain. Never invent customer names, parcel numbers, addresses, dates, times, values, or deadlines.
			
			For a HearingSchedule:
			- populate hearingSchedule;
			- set valueNotice to null.
			
			For a ValueNotice: 
			- populate valueNotice; 
			- set hearingSchedule to null.
			
			When documentType is "hearingSchedule":
			- hearingSchedule MUST be an object.
			- valueNotice MUST be null.
			
			When documentType is "valueNotice":
			- valueNotice MUST be an object.
			- hearingSchedule MUST be null.
			
			Never return objects for both hearingSchedule and valueNotice.
			Never return empty objects for an irrelevant document type.
			
			Return only the requested structured result.
			""";
	}

	private static string BuildUserPrompt(string documentText, string schema)
	{
		return
			$""" Analyze the following OCR text. --- BEGIN OCR TEXT --- {documentText} --- END OCR TEXT --- Return an object matching this JSON schema: {schema} """;
	}
}