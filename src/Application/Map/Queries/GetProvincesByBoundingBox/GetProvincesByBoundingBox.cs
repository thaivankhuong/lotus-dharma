using System.Runtime.CompilerServices;
using AutoMapper;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LotusDharma.Application.Map.Queries.GetProvincesByBoundingBox;

public sealed record GetProvincesByBoundingBoxQuery(
    double Xmin,
    double Ymin,
    double Xmax,
    double Ymax,
    double Simplify = 0,
    int Limit = 500) : IRequest<List<ProvinceGeoDto>>;

    public class GetProvincesByBoundingBoxQueryHandler : IRequestHandler<GetProvincesByBoundingBoxQuery, List<ProvinceGeoDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetProvincesByBoundingBoxQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ProvinceGeoDto>> Handle(GetProvincesByBoundingBoxQuery request, CancellationToken cancellationToken)
        {
            var tolerance = Math.Max(request.Simplify, 0);
            var limit = Math.Clamp(request.Limit, 1, 2000);

            var sql = tolerance > 0
                ? FormattableStringFactory.Create(@"
SELECT province_id,
       admin_code,
       name,
       administrative_center,
       area_km2,
       population,
       longitude,
       latitude,
       before_merger,
       administrative_units_info,
       province_34_id,
       province_code,
       ST_SimplifyPreserveTopology(geometry, {0}) AS geometry
FROM provinces
WHERE geometry && ST_MakeEnvelope({1}, {2}, {3}, {4}, 4326)
ORDER BY name
LIMIT {5}", tolerance, request.Xmin, request.Ymin, request.Xmax, request.Ymax, limit)
                : FormattableStringFactory.Create(@"
SELECT province_id,
       admin_code,
       name,
       administrative_center,
       area_km2,
       population,
       longitude,
       latitude,
       before_merger,
       administrative_units_info,
       province_34_id,
       province_code,
       geometry
FROM provinces
WHERE geometry && ST_MakeEnvelope({0}, {1}, {2}, {3}, 4326)
ORDER BY name
LIMIT {4}", request.Xmin, request.Ymin, request.Xmax, request.Ymax, limit);

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
    }

