using LotusDharma.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Search;

/// <summary>
/// Placeholder search service using database queries.
/// Replace with Elasticsearch or Azure Cognitive Search for production-scale full-text search.
/// </summary>
public class DatabaseSearchService : ISearchService
{
    private readonly ILogger<DatabaseSearchService> _logger;

    public DatabaseSearchService(ILogger<DatabaseSearchService> logger)
    {
        _logger = logger;
    }

    public Task IndexAsync<T>(string indexName, string documentId, T document, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Index operation on {IndexName}/{DocumentId} (no-op in database search mode)", indexName, documentId);
        return Task.CompletedTask;
    }

    public Task<SearchResult<T>> SearchAsync<T>(string indexName, string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Search on {IndexName} for '{Query}' (no-op in database search mode)", indexName, query);
        return Task.FromResult(new SearchResult<T>());
    }

    public Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Delete from {IndexName}/{DocumentId} (no-op in database search mode)", indexName, documentId);
        return Task.CompletedTask;
    }
}
