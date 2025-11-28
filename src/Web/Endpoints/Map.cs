using LotusDharma.Application.Geography.Dtos;
using LotusDharma.Application.Map.Queries.GetCommunesByBoundingBox;
using LotusDharma.Application.Map.Queries.GetProvincesByBoundingBox;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace LotusDharma.Web.Endpoints;

public class MapEndpoints : EndpointGroupBase
{
    public override string? GroupName => "map";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetFeaturesByBoundingBox, "bbox");
    }

    public async Task<Results<Ok<List<ProvinceGeoDto>>, Ok<List<CommuneGeoDto>>, BadRequest<string>>> GetFeaturesByBoundingBox(
        ISender sender,
        [AsParameters] MapBoundingBoxParameters parameters)
    {
        if (!TryParseBbox(parameters.Bbox, out var coords))
        {
            return TypedResults.BadRequest("The bbox parameter must be four comma-separated coordinates (xmin,ymin,xmax,ymax).");
        }

        var simplify = Math.Max(parameters.Simplify.GetValueOrDefault(0), 0);
        var limit = Math.Clamp(parameters.Limit ?? 500, 1, 3000);

        return parameters.Type?.Trim().ToLowerInvariant() switch
        {
            "province" => TypedResults.Ok(
                await sender.Send(new GetProvincesByBoundingBoxQuery(coords[0], coords[1], coords[2], coords[3], simplify, limit))),
            "commune" => TypedResults.Ok(
                await sender.Send(new GetCommunesByBoundingBoxQuery(coords[0], coords[1], coords[2], coords[3], simplify, limit))),
            _ => TypedResults.BadRequest("The type parameter must be either 'province' or 'commune'.")
        };
    }

    private static bool TryParseBbox(string? bbox, out double[] coords)
    {
        coords = Array.Empty<double>();

        if (string.IsNullOrWhiteSpace(bbox))
        {
            return false;
        }

        var parts = bbox.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
        {
            return false;
        }

        coords = new double[4];

        for (var i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                coords = Array.Empty<double>();
                return false;
            }

            coords[i] = value;
        }

        return coords[2] > coords[0] && coords[3] > coords[1];
    }
}

public record MapBoundingBoxParameters(string Type, string Bbox, double? Simplify = null, int? Limit = 500);

