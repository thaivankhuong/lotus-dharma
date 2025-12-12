using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

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
        if (string.IsNullOrWhiteSpace(_search))
            return query;

        var keyword = _search!;

        return query.Where(p =>
                     EF.Functions.ILike(p.Name, $"%{keyword}%") ||
                     EF.Functions.ILike(p.NameUnaccent ?? "", $"%{keyword}%") ||
                     EF.Functions.TrigramsSimilarity(p.NameUnaccent ?? "", keyword) > 0.3
                    );
    }
}



