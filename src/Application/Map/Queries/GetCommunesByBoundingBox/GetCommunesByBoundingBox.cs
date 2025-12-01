using AutoMapper;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace LotusDharma.Application.Map.Queries.GetCommunesByBoundingBox;

public sealed record GetCommunesByBoundingBoxQuery(
    double Xmin,
    double Ymin,
    double Xmax,
    double Ymax,
    double Simplify = 0,
    int Limit = 500) : IRequest<List<CommuneGeoDto>>;

public class GetCommunesByBoundingBoxQueryHandler : IRequestHandler<GetCommunesByBoundingBoxQuery, List<CommuneGeoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCommunesByBoundingBoxQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CommuneGeoDto>> Handle(GetCommunesByBoundingBoxQuery request, CancellationToken cancellationToken)
    {
        var tolerance = Math.Max(request.Simplify, 0);
        var limit = Math.Clamp(request.Limit, 1, 3000);

        var sql = tolerance > 0
            ? FormattableStringFactory.Create(@"
SELECT commune_id,
       province_id,
       commune_code,
       commune_3321_id,
       commune_internal_id,
       name,
       name_new,
       type,
       area_km2,
       population,
       longitude,
       latitude,
       before_merger,
       ST_SimplifyPreserveTopology(geometry, {0}) AS geometry,
       created_at,
       updated_at
FROM communes
WHERE geometry && ST_MakeEnvelope({1}, {2}, {3}, {4}, 4326)
ORDER BY name
LIMIT {5}", tolerance, request.Xmin, request.Ymin, request.Xmax, request.Ymax, limit)
            : FormattableStringFactory.Create(@"
SELECT commune_id,
       province_id,
       commune_code,
       commune_3321_id,
       commune_internal_id,
       name,
       name_new,
       type,
       area_km2,
       population,
       longitude,
       latitude,
       before_merger,
       geometry,
       created_at,
       updated_at
FROM communes
WHERE geometry && ST_MakeEnvelope({0}, {1}, {2}, {3}, 4326)
ORDER BY name
LIMIT {4}", request.Xmin, request.Ymin, request.Xmax, request.Ymax, limit);

        var communes = await _context.Communes
            .FromSqlInterpolated(sql)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return communes.Select(c =>
        {
            var dto = _mapper.Map<CommuneGeoDto>(c);
            dto.Geometry = c.Geometry.ToGeoJson();
            return dto;
        }).ToList();
    }
}

