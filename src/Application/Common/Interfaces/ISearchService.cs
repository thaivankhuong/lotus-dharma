namespace LotusDharma.Application.Common.Interfaces;

public interface ISearchService
{
    Task IndexAsync<T>(string indexName, string documentId, T document, CancellationToken cancellationToken = default) where T : class;
    Task<SearchResult<T>> SearchAsync<T>(string indexName, string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) where T : class;
    Task DeleteAsync(string indexName, string documentId, CancellationToken cancellationToken = default);
}

public class SearchResult<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
