using Ardalis.GuardClauses;
using AutoMapper;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite;

namespace LotusDharma.Application.Communes.Queries.GetCommuneById;

public sealed record GetCommuneByIdQuery(string CommuneId, double? Simplify = null) : IRequest<CommuneGeoDto>;

public class GetCommuneByIdQueryHandler : IRequestHandler<GetCommuneByIdQuery, CommuneGeoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCommuneByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CommuneGeoDto> Handle(GetCommuneByIdQuery request, CancellationToken cancellationToken)
    {
        var tolerance = Math.Max(request.Simplify.GetValueOrDefault(0), 0);

        Commune? commune;

        if (tolerance > 0)
        {
            FormattableString sql = $@"
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
            WHERE commune_id = {request.CommuneId}
            LIMIT 1;
        ";

            commune = await _context.Communes
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            commune = await _context.Communes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CommuneId == request.CommuneId, cancellationToken);
        }

        if (commune is null)
            throw new KeyNotFoundException(request.CommuneId);

        // Mapping DTO
        var dto = _mapper.Map<CommuneGeoDto>(commune);
        dto.Geometry = commune.Geometry.ToGeoJson();

        return dto;
    }

}

