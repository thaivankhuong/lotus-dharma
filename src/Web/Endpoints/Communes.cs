using LotusDharma.Application.Communes.Queries.GetCommuneById;
using LotusDharma.Application.Communes.Queries.GetCommunes;
using LotusDharma.Application.Geography.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LotusDharma.Web.Endpoints;

public class Communes : EndpointGroupBase
{
    public override string? GroupName => "communes";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCommunes);
        groupBuilder.MapGet(GetCommuneById, "{id}");
    }

    public async Task<Ok<List<CommuneGeoDto>>> GetCommunes(ISender sender, [AsParameters] GetCommunesQuery query)
    {
        var communes = await sender.Send(query);
        return TypedResults.Ok(communes);
    }

    public async Task<Results<Ok<CommuneGeoDto>, NotFound>> GetCommuneById(ISender sender, string id, [FromQuery] double? simplify)
    {
        try
        {
            var commune = await sender.Send(new GetCommuneByIdQuery(id, simplify));
            return TypedResults.Ok(commune);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}


