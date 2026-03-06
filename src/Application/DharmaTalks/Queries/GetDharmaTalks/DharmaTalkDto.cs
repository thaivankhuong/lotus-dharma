using AutoMapper;
using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.DharmaTalks.Queries.GetDharmaTalks;

public class DharmaTalkDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public int SpeakerId { get; set; }
    public string? SeriesTitle { get; set; }
    public int? SeriesId { get; set; }
    public int? SeriesOrder { get; set; }
    public string? AudioUrl { get; set; }
    public int DurationSeconds { get; set; }
    public long AudioSizeBytes { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public long PlayCount { get; set; }
    public string? Tags { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DharmaTalk, DharmaTalkDto>()
                .ForMember(d => d.SpeakerName, opt => opt.MapFrom(s => s.Speaker.Name))
                .ForMember(d => d.SeriesTitle, opt => opt.MapFrom(s => s.Series != null ? s.Series.Title : null));
        }
    }
}
