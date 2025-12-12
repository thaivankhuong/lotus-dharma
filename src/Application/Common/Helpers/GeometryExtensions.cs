using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LotusDharma.Application.Common.Helpers;

public static class GeometryExtensions
{
    public static string? ToGeoJson(this Geometry? geometry)
    {
        if (geometry is null)
        {
            return null;
        }

        var writer = new GeoJsonWriter();
        return writer.Write(geometry);
    }
}











