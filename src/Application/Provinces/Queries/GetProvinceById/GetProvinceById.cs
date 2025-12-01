using Ardalis.GuardClauses;
using AutoMapper;
using LotusDharma.Application.Common.Helpers;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite;

namespace LotusDharma.Application.Provinces.Queries.GetProvinceById;

public sealed record GetProvinceByIdQuery(int ProvinceId, double? Simplify = null) : IRequest<ProvinceGeoDto>;

public class GetProvinceByIdQueryHandler : IRequestHandler<GetProvinceByIdQuery, ProvinceGeoDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProvinceByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ProvinceGeoDto> Handle(GetProvinceByIdQuery request, CancellationToken cancellationToken)
    {
        var tolerance = Math.Max(request.Simplify.GetValueOrDefault(0), 0);

        Province? province;

        if (tolerance > 0)
        {
            FormattableString sql = $@"
            SELECT 
                province_id,
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
                ST_SimplifyPreserveTopology(geometry, {tolerance}) AS geometry
            FROM provinces
            WHERE province_id = {request.ProvinceId}
            LIMIT 1;
        ";

            province = await _context.Provinces
                .FromSqlInterpolated(sql)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            province = await _context.Provinces
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProvinceId, cancellationToken);
        }

        if (province is null)
            throw new KeyNotFoundException(request.ProvinceId.ToString());

        // Mapping sang DTO
        var dto = _mapper.Map<ProvinceGeoDto>(province);
        dto.Geometry = province.Geometry.ToGeoJson();

        return dto;
    }

}

