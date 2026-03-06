namespace LotusDharma.Domain.Entities;

public class DharmaTalk : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Slug { get; set; }

    public int SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;

    public int? SeriesId { get; set; }
    public DharmaTalkSeries? Series { get; set; }

    public int? SeriesOrder { get; set; }

    public string AudioBlobName { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public long AudioSizeBytes { get; set; }
    public int DurationSeconds { get; set; }
    public string AudioContentType { get; set; } = "audio/mpeg";

    public string? CoverImageUrl { get; set; }
    public string? TranscriptText { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public long PlayCount { get; set; }

    public string? Tags { get; set; }
}
