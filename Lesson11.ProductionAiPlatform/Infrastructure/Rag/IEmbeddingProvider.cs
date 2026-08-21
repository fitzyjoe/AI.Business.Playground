using Microsoft.Extensions.AI;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Rag;

public interface IEmbeddingProvider
{
	string Name { get; }

	IEmbeddingGenerator<string, Embedding<float>> CreateGenerator();
}