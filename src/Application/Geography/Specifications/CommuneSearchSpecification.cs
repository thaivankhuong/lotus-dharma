using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Geography.Specifications;

public sealed class CommuneSearchSpecification
{
    private readonly string? _search;

    public CommuneSearchSpecification(string? search)
    {
        _search = string.IsNullOrWhiteSpace(search) ? null : search!.Trim();
    }

    public IQueryable<Commune> Apply(IQueryable<Commune> query)
    {
        if (_search is null)
        {
            return query;
        }

        var lowered = _search.ToLowerInvariant();
        return query.Where(c =>
            c.Name.ToLower().Contains(lowered) ||
            (c.NameNew ?? string.Empty).ToLower().Contains(lowered) ||
            (c.Type ?? string.Empty).ToLower().Contains(lowered));
    }
}



