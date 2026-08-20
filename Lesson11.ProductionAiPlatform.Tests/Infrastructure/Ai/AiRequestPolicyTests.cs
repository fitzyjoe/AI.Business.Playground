using Lesson11.ProductionAiPlatform.Features.Conversations;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Tests.Infrastructure.Ai;

public sealed class AiRequestPolicyTests
{
	[Fact]
	public void ResolveNewConversationUsesConfiguredDefaults()
	{
		var policy = CreatePolicy();
		var request = new MessageRequest
		{
			Content = "Hello"
		};

		var resolved = policy.ResolveNewConversation(request);

		Assert.Equal("openai", resolved.Provider);
		Assert.Equal(0f, resolved.Temperature);
		Assert.Equal(600, resolved.MaxOutputTokens);
	}

	[Fact]
	public void ResolveNewConversationClampsMaxTokens()
	{
		var policy = CreatePolicy(maxOutputTokens: 1_200);
		var request = new MessageRequest
		{
			Content = "Hello",
			MaxTokens = 10_000
		};

		var resolved = policy.ResolveNewConversation(request);

		Assert.Equal(1_200, resolved.MaxOutputTokens);
	}

	[Fact]
	public void ResolveNewConversationRejectsUnsupportedProvider()
	{
		var policy = CreatePolicy(allowedProviders: ["openai"]);
		var request = new MessageRequest
		{
			Content = "Hello",
			Provider = "not-allowed"
		};

		var exception = Assert.Throws<AiPolicyViolationException>(
			() => policy.ResolveNewConversation(request));

		Assert.Contains("not-allowed", exception.Message);
	}

	[Fact]
	public void ResolveNewConversationRejectsTemperatureAboveApplicationLimit()
	{
		var policy = CreatePolicy(maxTemperature: 1.0f);
		var request = new MessageRequest
		{
			Content = "Hello",
			Temperature = 1.5f
		};

		Assert.Throws<AiPolicyViolationException>(
			() => policy.ResolveNewConversation(request));
	}

	[Fact]
	public void ResolveNewConversationRejectsOversizedInput()
	{
		var policy = CreatePolicy(maxInputCharacters: 5);
		var request = new MessageRequest
		{
			Content = "123456"
		};

		Assert.Throws<AiPolicyViolationException>(
			() => policy.ResolveNewConversation(request));
	}

	[Fact]
	public void ValidateExistingConversationRejectsProviderRemovedFromAllowlist()
	{
		var policy = CreatePolicy(allowedProviders: ["openai"]);
		var request = new MessageRequest
		{
			ConversationId = Guid.NewGuid(),
			Content = "Continue"
		};
		var conversation = new Conversation
		{
			OwnerId = "reader-user",
			Provider = "ollama"
		};

		Assert.Throws<AiPolicyViolationException>(
			() => policy.ValidateExistingConversation(request, conversation));
	}

	private static AiRequestPolicy CreatePolicy(
		string[]? allowedProviders = null,
		int maxInputCharacters = 100,
		int defaultMaxOutputTokens = 600,
		int maxOutputTokens = 1_200,
		float maxTemperature = 1.0f)
	{
		return new AiRequestPolicy(
			Options.Create(
				new AiOptions
				{
					DefaultProvider = "openai",
					AllowedProviders = allowedProviders ?? ["openai", "ollama"],
					MaxInputCharacters = maxInputCharacters,
					DefaultMaxOutputTokens = defaultMaxOutputTokens,
					MaxOutputTokens = maxOutputTokens,
					MaxTemperature = maxTemperature,
					AgentRequestTimeoutSeconds = 90,
					ProviderCallTimeoutSeconds = 45,
					MaxConcurrentCallsPerProvider = 4
				}));
	}
}
