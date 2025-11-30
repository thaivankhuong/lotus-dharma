using System;

namespace LotusDharma.Application.Geography.Dtos;

public class CommuneDto
{
    public string CommuneId { get; set; } = string.Empty;
    public string ProvinceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameNew { get; set; }
    public string? Type { get; set; }
    public double? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? BeforeMerger { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}



