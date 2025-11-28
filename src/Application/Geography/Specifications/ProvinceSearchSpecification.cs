using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Geography.Specifications;

public sealed class ProvinceSearchSpecification
{
    private readonly string? _search;

    public ProvinceSearchSpecification(string? search)
    {
        _search = string.IsNullOrWhiteSpace(search) ? null : search!.Trim();
    }

    public IQueryable<Province> Apply(IQueryable<Province> query)
    {
        if (_search is null)
        {
            return query;
        }

        var lowered = _search.ToLowerInvariant();
        return query.Where(p =>
            p.Name.ToLower().Contains(lowered) ||
            (p.NameNew ?? string.Empty).ToLower().Contains(lowered));
    }
}


