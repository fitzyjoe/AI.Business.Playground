using Lesson10.MonitoringAndAnomalyDetection.Features.Agents;
using Microsoft.Agents.AI;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Conversations;

public sealed class MessageHandler(
	IConversationRepository _conversationRepository,
	PropertyReviewAgent _propertyReviewAgent)
{
	public async Task<MessageResponse> HandleAsync(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		Conversation conversation;
		AgentSession session;

		if (messageRequest.ConversationId.HasValue)
		{
			conversation = await _conversationRepository.GetAsync(
				messageRequest.ConversationId.Value,
				cancellationToken)
			?? throw new ConversationNotFoundException(
				messageRequest.ConversationId.Value);

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
			session = await _propertyReviewAgent.CreateSessionAsync(
				conversation,
				cancellationToken);
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

		await _conversationRepository.SaveAsync(
			conversation,
			cancellationToken);

		return new MessageResponse(
			conversation.Id,
			agentResponse.Text,
			agentResponse.Model,
			agentResponse.Duration);
	}

	private static Conversation CreateConversation(MessageRequest request)
	{
		return new Conversation
		{
			SystemPrompt = request.SystemPrompt,
			Provider = request.Provider ?? "ollama",
			Model = request.Model,
			Temperature = request.Temperature,
			MaxTokens = request.MaxTokens
		};
	}
}