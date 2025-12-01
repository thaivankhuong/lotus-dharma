using System;
using System.Collections.Generic;

namespace LotusDharma.Application.Geography.Dtos;

public class CommuneDto
{
    public int CommuneId { get; set; }
    public int ProvinceId { get; set; }
    public string CommuneCode { get; set; } = string.Empty;
    public string Commune3321Id { get; set; } = string.Empty;
    public string CommuneInternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameNew { get; set; }
    public string? Type { get; set; }
    public double? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? BeforeMerger { get; set; }
    public List<double>? Bbox { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}



