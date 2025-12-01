using System.Collections.Generic;

namespace LotusDharma.Application.Geography.Dtos;

public class ProvinceDto
{
    public int ProvinceId { get; set; }
    public int AdminCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AdministrativeCenter { get; set; }
    public double AreaKm2 { get; set; }
    public long Population { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public string BeforeMerger { get; set; } = string.Empty;
    public string AdministrativeUnitsInfo { get; set; } = string.Empty;
    public string Province34Id { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public List<double>? Bbox { get; set; }
}



