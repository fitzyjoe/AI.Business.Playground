using Microsoft.Extensions.AI;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Rag;

public interface IEmbeddingGeneratorFactory
{
	IEmbeddingGenerator<string, Embedding<float>> GetGenerator(string provider);
}