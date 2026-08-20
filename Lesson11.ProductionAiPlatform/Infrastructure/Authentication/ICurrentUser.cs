namespace Lesson11.ProductionAiPlatform.Infrastructure.Authentication;

public interface ICurrentUser
{
	string Id { get; }
	string Name { get; }
	bool IsInRole(string role);
}