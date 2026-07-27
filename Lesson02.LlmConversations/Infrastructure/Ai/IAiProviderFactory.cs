namespace Lesson01.Chat.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string modelName);
}
