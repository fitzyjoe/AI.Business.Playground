using System.ComponentModel;
using Lesson09.Agents.Infrastructure.Rag;

namespace Lesson09.Agents.Features.Knowledge;

public sealed class KnowledgeTools(KnowledgeRetriever _knowledgeRetriever)
{
	[Description(
		"Searches the company's internal property-tax knowledge base for procedures, policies, guidance, " +
		"hearing preparation, valuation guidance, and client communication information.")]
	public Task<IReadOnlyList<KnowledgeSearchResult>> SearchInternalKnowledgeAsync(
		[Description("The question or topic to search for.")] string query,
		CancellationToken cancellationToken)
	{
		return _knowledgeRetriever.SearchAsync(query, cancellationToken);
	}
}