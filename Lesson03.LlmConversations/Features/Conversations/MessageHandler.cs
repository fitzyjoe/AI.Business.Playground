using Lesson03.LlmConversations.Infrastructure.Ai;

namespace Lesson03.LlmConversations.Features.Conversations;

public sealed class MessageHandler(
	IConversationRepository conversationRepository,
	IAiProviderFactory aiProviderFactory)
{
	public async Task<MessageResponse> HandleAsync(
		MessageRequest messageRequest,
		CancellationToken cancellationToken)
	{
		var userMessage = new ConversationMessage
		{
			Role = ConversationRole.User,
			Content = messageRequest.Content,
			CreatedAt = DateTimeOffset.UtcNow
		};
		
		Conversation conversation;
		
		if (messageRequest.ConversationId.HasValue)
		{
			var possibleConversation = await conversationRepository.GetAsync(
				messageRequest.ConversationId.Value, 
				cancellationToken);
			conversation = possibleConversation ?? throw new Exception("No conversation found for supplied conversationId " +
			                                                           messageRequest.ConversationId.Value);
		}
		else
		{
			conversation = CreateConversation(messageRequest);
		}
		
		var aiRequest = new AiChatRequest
		{
			Messages = BuildMessages(
				conversation,
				userMessage),
			Model = conversation.Model,
			Temperature = conversation.Temperature,
			MaxTokens = conversation.MaxTokens
		};
		
		var aiProvider = aiProviderFactory.GetProvider(conversation.Provider);

		var aiChatResponse = await aiProvider.SendAsync(
			aiRequest,
			cancellationToken);
		
		var assistantMessage = new ConversationMessage
		{
			Role = ConversationRole.Assistant,
			Content = aiChatResponse.Text,
			CreatedAt = DateTimeOffset.UtcNow
		};
		
		// Persist the turn only after the AI call succeeds.
		conversation.Messages.Add(userMessage);
		conversation.Messages.Add(assistantMessage);
		conversation.UpdatedAt = assistantMessage.CreatedAt;
		
		await conversationRepository.SaveAsync(
			conversation,
			cancellationToken);
		
		return new MessageResponse(
			conversation.Id,
			assistantMessage.Content,
			aiChatResponse.Model,
			aiChatResponse.Duration);
	}
	
	private static IReadOnlyList<ConversationMessage> BuildMessages(
		Conversation conversation,
		ConversationMessage pendingUserMessage)
	{
		var messages = new List<ConversationMessage>
		{
			new()
			{
				Role = ConversationRole.System,
				Content = conversation.SystemPrompt,
				CreatedAt = conversation.CreatedAt
			}
		};

		messages.AddRange(conversation.Messages);
		messages.Add(pendingUserMessage);

		return messages;
	}

	private static Conversation CreateConversation(MessageRequest request)
	{
		return new Conversation
		{
			SystemPrompt = request.SystemPrompt ?? "You are a helpful assistant.",
			Provider = request.Provider,
			Model = request.Model,
			Temperature = request.Temperature,
			MaxTokens = request.MaxTokens
		};
	}
}