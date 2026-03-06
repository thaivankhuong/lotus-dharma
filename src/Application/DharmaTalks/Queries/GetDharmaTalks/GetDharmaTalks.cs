using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Mappings;
using LotusDharma.Application.Common.Models;

namespace LotusDharma.Application.DharmaTalks.Queries.GetDharmaTalks;

public record GetDharmaTalksQuery : IRequest<PaginatedList<DharmaTalkDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? SpeakerId { get; init; }
    public int? SeriesId { get; init; }
    public string? Search { get; init; }
}

public class GetDharmaTalksQueryHandler : IRequestHandler<GetDharmaTalksQuery, PaginatedList<DharmaTalkDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDharmaTalksQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<DharmaTalkDto>> Handle(GetDharmaTalksQuery request, CancellationToken cancellationToken)
    {
        var query = _context.DharmaTalks
            .AsNoTracking()
            .Include(t => t.Speaker)
            .Include(t => t.Series)
            .Where(t => t.IsPublished)
            .AsQueryable();

        if (request.SpeakerId.HasValue)
            query = query.Where(t => t.SpeakerId == request.SpeakerId.Value);

        if (request.SeriesId.HasValue)
            query = query.Where(t => t.SeriesId == request.SeriesId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(t =>
                t.Title.ToLower().Contains(search) ||
                (t.Description ?? "").ToLower().Contains(search) ||
                t.Speaker.Name.ToLower().Contains(search));
        }

        query = query.OrderByDescending(t => t.PublishedAt);

        return await query
            .ProjectTo<DharmaTalkDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
