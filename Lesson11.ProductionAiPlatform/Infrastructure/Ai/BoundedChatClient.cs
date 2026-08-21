using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class BoundedChatClient : DelegatingChatClient
{
    private readonly AiOptions _options;
    private readonly SemaphoreSlim _concurrencyGate;

    public BoundedChatClient(IChatClient innerClient, IOptions<AiOptions> options) : base(innerClient)
    {
        _options = options.Value;

        _concurrencyGate = new SemaphoreSlim(
            _options.MaxConcurrentCallsPerProvider,
            _options.MaxConcurrentCallsPerProvider);
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options = ApplyLimits(options);

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(_options.ProviderCallTimeoutSeconds));

        var acquired = false;
        
        try
        {
            await _concurrencyGate.WaitAsync(timeoutCancellation.Token);
            acquired = true;
            return await base.GetResponseAsync(messages, options, timeoutCancellation.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"AI provider call exceeded the {_options.ProviderCallTimeoutSeconds}-second limit.");
        }
        finally
        {
            if (acquired)
            {
                _concurrencyGate.Release();
            }
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options = ApplyLimits(options);

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(_options.ProviderCallTimeoutSeconds));

        var acquired = false;

        try
        {
            await _concurrencyGate.WaitAsync(timeoutCancellation.Token);
            acquired = true;

            await foreach (var update in base.GetStreamingResponseAsync(
                               messages,
                               options,
                               timeoutCancellation.Token))
            {
                yield return update;
            }
        }
        finally
        {
            if (acquired)
            {
                _concurrencyGate.Release();
            }
        }
    }

    private ChatOptions ApplyLimits(ChatOptions? options)
    {
        options ??= new ChatOptions();
        var requestedMaxTokens = options.MaxOutputTokens ?? _options.DefaultMaxOutputTokens;
        options.MaxOutputTokens = Math.Min(requestedMaxTokens, _options.MaxOutputTokens);
        return options;
    }
}