using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;

namespace Lesson11.ProductionAiPlatform.Features.Knowledge;

public sealed class KnowledgeRetriever(
    VectorStore _vectorStore,
    IHostEnvironment _environment,
    IOptions<RagOptions> _options)
{
    private VectorStoreCollection<string, KnowledgeChunk>? _collection;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var definition = new VectorStoreCollectionDefinition
        {
            Properties = new List<VectorStoreProperty>
            {
                new VectorStoreKeyProperty(nameof(KnowledgeChunk.Id), typeof(string)),
                new VectorStoreDataProperty(nameof(KnowledgeChunk.Source), typeof(string)),
                new VectorStoreDataProperty(nameof(KnowledgeChunk.Content), typeof(string)),
                new VectorStoreVectorProperty(nameof(KnowledgeChunk.Embedding), typeof(string), _options.Value.EmbeddingDimensions)
                {
                    DistanceFunction = DistanceFunction.CosineSimilarity
                }
            }
        };
        
        _collection = _vectorStore.GetCollection<string, KnowledgeChunk>("knowledge", definition);

        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        var knowledgePath = Path.Combine(_environment.ContentRootPath, "Knowledge");

        foreach (var path in Directory.GetFiles(knowledgePath, "*.md"))
        {
            var text = await File.ReadAllTextAsync(path, cancellationToken);
            var source = Path.GetFileName(path);
            var index = 0;

            foreach (var content in SplitIntoChunks(text))
            {
                await _collection.UpsertAsync(
                    new KnowledgeChunk
                    {
                        Id = $"{source}:{index++}",
                        Source = source,
                        Content = content
                    },
                    cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_collection is null)
        {
            throw new InvalidOperationException("The knowledge base has not been initialized.");
        }

        var results = new List<KnowledgeSearchResult>();

        var searchResults = _collection.SearchAsync(query, _options.Value.TopResults, cancellationToken: cancellationToken);
        await foreach (var result in searchResults)
        {
            results.Add(
                new KnowledgeSearchResult(
                    result.Record.Source,
                    result.Record.Content,
                    result.Score));
        }

        return results;
    }

    // if we knew the format for the documents we were indexing, we might be smarter about the chunking... for now I've left it to chunk by paragraphs.
    private static IEnumerable<string> SplitIntoChunks(string document)
    {
        return document.Split(
            "\n\n",
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }
}