using Microsoft.Extensions.AI;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public interface IAiProvider
{
	string Name { get; }
	string DefaultModel { get; }
	IChatClient ChatClient { get; }
}