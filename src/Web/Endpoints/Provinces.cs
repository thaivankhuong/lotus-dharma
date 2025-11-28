using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Application.Provinces.Queries.GetProvinceById;
using LotusDharma.Application.Provinces.Queries.GetProvinces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LotusDharma.Web.Endpoints;

public class Provinces : EndpointGroupBase
{
    public override string? GroupName => "provinces";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetProvinces);
        groupBuilder.MapGet(GetProvinceById, "{id}");
    }

    public async Task<Ok<List<ProvinceGeoDto>>> GetProvinces(ISender sender, [AsParameters] GetProvincesQuery query)
    {
        var provinces = await sender.Send(query);
        return TypedResults.Ok(provinces);
    }

    public async Task<Results<Ok<ProvinceGeoDto>, NotFound>> GetProvinceById(ISender sender, string id, [FromQuery] double? simplify)
    {
        try
        {
            var province = await sender.Send(new GetProvinceByIdQuery(id, simplify));
            return TypedResults.Ok(province);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}


