using Lesson11.ProductionAiPlatform.Features.Agents;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Lesson11.ProductionAiPlatform.Infrastructure.Authentication;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public sealed class MessageHandler(
	IConversationRepository _conversationRepository,
	PropertyReviewAgent _propertyReviewAgent,
	AiRequestPolicy _requestPolicy,
	ICurrentUser _currentUser,
	IOptions<AiOptions> _aiOptions)
{
	public async Task<MessageResponse> HandleAsync(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(_aiOptions.Value.AgentRequestTimeoutSeconds));

		try
		{
			return await HandleCoreAsync(messageRequest, timeoutCancellation.Token);
		}
		catch (OperationCanceledException)
			when (!cancellationToken.IsCancellationRequested)
		{
			throw new AiRequestTimeoutException(
				$"AI request exceeded the {_aiOptions.Value.AgentRequestTimeoutSeconds}-second limit.");
		}
		catch (TimeoutException exception)
		{
			throw new AiRequestTimeoutException(exception.Message);
		}
	}

	private async Task<MessageResponse> HandleCoreAsync(
		MessageRequest messageRequest,
		CancellationToken cancellationToken)
	{
		Conversation conversation;
		AgentSession session;

		if (messageRequest.ConversationId.HasValue)
		{
			conversation = await _conversationRepository.GetAsync(
				messageRequest.ConversationId.Value,
				_currentUser.Id,
				cancellationToken)
			?? throw new ConversationNotFoundException(
				messageRequest.ConversationId.Value);

			_requestPolicy.ValidateExistingConversation(messageRequest, conversation);

			if (!conversation.AgentSessionState.HasValue)
			{
				throw new InvalidOperationException(
					$"Conversation '{conversation.Id}' does not contain agent session state.");
			}

			session = await _propertyReviewAgent.DeserializeSessionAsync(
				conversation,
				conversation.AgentSessionState.Value,
				cancellationToken);
		}
		else
		{
			conversation = CreateConversation(messageRequest);
			session = await _propertyReviewAgent.CreateSessionAsync(conversation, cancellationToken);
		}

		var agentResponse = await _propertyReviewAgent.RunAsync(
			messageRequest.Content,
			session,
			conversation,
			cancellationToken);

		conversation.AgentSessionState = await _propertyReviewAgent.SerializeSessionAsync(
			conversation,
			session,
			cancellationToken);

		conversation.UpdatedAt = DateTimeOffset.UtcNow;

		await _conversationRepository.SaveAsync(conversation, cancellationToken);

		return new MessageResponse(
			conversation.Id,
			agentResponse.Text,
			agentResponse.Model,
			agentResponse.Duration);
	}

	private Conversation CreateConversation(MessageRequest request)
	{
		var resolved = _requestPolicy.ResolveNewConversation(request);

		return new Conversation
		{
			OwnerId = _currentUser.Id,
			Provider = resolved.Provider,
			Temperature = resolved.Temperature,
			MaxTokens = resolved.MaxOutputTokens
		};
	}
}
