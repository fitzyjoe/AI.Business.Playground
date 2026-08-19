using Microsoft.Extensions.Options;
using Lesson11.ProductionAiPlatform.Features.Conversations;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class AiRequestPolicy(IOptions<ProductionAiOptions> _options)
{
    public ResolvedAiRequestOptions ResolveNewConversation(MessageRequest request)
    {
        ValidateContent(request.Content);

        var options = _options.Value;

        var provider = request.Provider ?? options.DefaultProvider;
        if (!options.AllowedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            throw new AiPolicyViolationException($"AI provider '{provider}' is not allowed.");
        }

        var temperature = request.Temperature ?? 0;
        if (temperature > options.MaxTemperature)
        {
            throw new AiPolicyViolationException($"Temperature cannot exceed {options.MaxTemperature}.");
        }

        var maxTokens = Math.Min(request.MaxTokens ?? options.DefaultMaxOutputTokens, options.MaxOutputTokens);

        return new ResolvedAiRequestOptions(provider, temperature, maxTokens);
    }

    public void ValidateExistingConversation(MessageRequest request, Conversation conversation)
    {
        ValidateContent(request.Content);

        if (!_options.Value.AllowedProviders.Contains(conversation.Provider, StringComparer.OrdinalIgnoreCase))
        {
            throw new AiPolicyViolationException($"AI provider '{conversation.Provider}' is no longer allowed.");
        }
    }

    private void ValidateContent(string content)
    {
        if (content.Length > _options.Value.MaxInputCharacters)
        {
            throw new AiPolicyViolationException($"Message content cannot exceed {_options.Value.MaxInputCharacters} characters.");
        }
    }
}

public sealed record ResolvedAiRequestOptions(
    string Provider,
    float Temperature,
    int MaxOutputTokens);

public sealed class AiPolicyViolationException(string message) : Exception(message);