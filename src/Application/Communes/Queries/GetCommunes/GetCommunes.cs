using System.Runtime.CompilerServices;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Application.Geography.Specifications;
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite;

namespace LotusDharma.Application.Communes.Queries.GetCommunes;

public sealed record GetCommunesQuery(
    string? ProvinceId = null,
    string? Search = null,
    bool IncludeGeometry = false,
    double? Simplify = null,
    int Limit = 300) : IRequest<List<CommuneGeoDto>>;

public class GetCommunesQueryHandler : IRequestHandler<GetCommunesQuery, List<CommuneGeoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCommunesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CommuneGeoDto>> Handle(GetCommunesQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Limit, 1, 1500);
        var tolerance = Math.Max(request.Simplify.GetValueOrDefault(0), 0);

        if (!request.IncludeGeometry)
        {
            var query = _context.Communes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.ProvinceId))
                query = query.Where(c => c.ProvinceId == request.ProvinceId);

            query = new CommuneSearchSpecification(request.Search).Apply(query);

            return await query
                .OrderBy(c => c.Name)
                .Take(take)
                .ProjectTo<CommuneGeoDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        if (tolerance > 0)
        {
            FormattableString sql = FormattableStringFactory.Create($@"
                SELECT 
                    commune_id,
                    province_id,
                    name,
                    name_new,
                    type,
                    area_km2,
                    population,
                    longitude,
                    latitude,
                    before_merger,
                    created_at,
                    updated_at,
                    ST_SimplifyPreserveTopology(geometry, {tolerance}) AS geometry
                FROM communes
                {(!string.IsNullOrWhiteSpace(request.ProvinceId) ? $"WHERE province_id = {request.ProvinceId}" : "")}
                ORDER BY name
                LIMIT {take};
            ");
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

        var normalQuery = _context.Communes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.ProvinceId))
            normalQuery = normalQuery.Where(c => c.ProvinceId == request.ProvinceId);

        normalQuery = new CommuneSearchSpecification(request.Search).Apply(normalQuery);

        var list = await normalQuery
            .OrderBy(c => c.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        return list.Select(c =>
        {
            var dto = _mapper.Map<CommuneGeoDto>(c);
            dto.Geometry = c.Geometry.ToGeoJson();
            return dto;
        }).ToList();
    }

}

