using AutoMapper;
using AutoMapper.QueryableExtensions;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Application.Geography.Specifications;
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite;

namespace LotusDharma.Application.Provinces.Queries.GetProvinces;

public sealed record GetProvincesQuery(
    string? Search = null,
    bool IncludeGeometry = false,
    double? Simplify = null,
    int Limit = 200) : IRequest<List<ProvinceGeoDto>>;

public class GetProvincesQueryHandler : IRequestHandler<GetProvincesQuery, List<ProvinceGeoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProvincesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ProvinceGeoDto>> Handle(GetProvincesQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Limit, 1, 1000);
        var tolerance = Math.Max(request.Simplify.GetValueOrDefault(0), 0);

        if (!request.IncludeGeometry)
        {
            var query = _context.Provinces.AsNoTracking();
            query = new ProvinceSearchSpecification(request.Search).Apply(query);

            return await query
                .OrderBy(p => p.Name)
                .Take(take)
                .ProjectTo<ProvinceGeoDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        if (tolerance > 0)
        {
            FormattableString sql = $@"
            SELECT 
                province_id,
                name,
                name_new,
                administrative_center,
                area_km2,
                population,
                longitude,
                latitude,
                before_merger,
                administrative_units,
                created_at,
                updated_at,
                ST_SimplifyPreserveTopology(geometry, {tolerance}) AS geometry
            FROM provinces
            ORDER BY name
            LIMIT {take};
        ";

            var provinces = await _context.Provinces
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return provinces.Select(p =>
            {
                var dto = _mapper.Map<ProvinceGeoDto>(p);
                dto.Geometry = p.Geometry.ToGeoJson();
                return dto;
            }).ToList();
        }

        var normalQuery = _context.Provinces.AsNoTracking();
        normalQuery = new ProvinceSearchSpecification(request.Search).Apply(normalQuery);

        var list = await normalQuery
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return list.Select(p =>
        {
            var dto = _mapper.Map<ProvinceGeoDto>(p);
            dto.Geometry = p.Geometry.ToGeoJson();
            return dto;
        }).ToList();
    }

}

