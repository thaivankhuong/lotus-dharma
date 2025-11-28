using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace LotusDharma.Domain.Entities;

public class Province
{
    public string ProvinceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameNew { get; set; }
    public string? AdministrativeCenter { get; set; }
    public double? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? BeforeMerger { get; set; }
    public string? AdministrativeUnits { get; set; }
    public MultiPolygon? Geometry { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Commune> Communes { get; set; } = new List<Commune>();
}


