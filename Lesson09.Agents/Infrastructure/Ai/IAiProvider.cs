using Microsoft.Extensions.AI;

namespace Lesson09.Agents.Infrastructure.Ai;

public interface IAiProvider
{
	string Name { get; }
	string DefaultModel { get; }
	IChatClient ChatClient { get; }
}