using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace LotusDharma.Domain.Entities;

public class Province
{
    public int Id { get; set; }
    public int AdminCode { get; set; }
    public string Name { get; set; } = string.Empty;

    public double AreaKm2 { get; set; }
    public long Population { get; set; }

    public string AdministrativeCenter { get; set; } = string.Empty;

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public string BeforeMerger { get; set; } = string.Empty;

    public string AdministrativeUnitsInfo { get; set; } = string.Empty;

    public string Province34Id { get; set; } = string.Empty;

    public string ProvinceCode { get; set; } = string.Empty;

    public MultiPolygon? Geometry { get; set; }
    public List<double>? Bbox { get; set; }
    public string? NameUnaccent { get; set; }

    public ICollection<Commune> Communes { get; set; } = new List<Commune>();
}












