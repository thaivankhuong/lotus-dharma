using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;
using LotusDharma.Application.DharmaTalks.Queries.GetDharmaTalks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace LotusDharma.Web.Endpoints;

public class DharmaTalks : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetDharmaTalks)
            .RequireRateLimiting("public");

        groupBuilder.MapGet(StreamAudio, "{id}/stream")
            .RequireRateLimiting("public");
    }

    public async Task<Ok<PaginatedList<DharmaTalkDto>>> GetDharmaTalks(
        ISender sender,
        [AsParameters] GetDharmaTalksQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<IResult> StreamAudio(
        int id,
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var talk = await context.DharmaTalks.FindAsync(new object[] { id }, cancellationToken);

        if (talk == null || !talk.IsPublished)
            return Results.NotFound();

        var rangeHeader = httpContext.Request.Headers.Range.FirstOrDefault();

        if (!string.IsNullOrEmpty(rangeHeader))
        {
            return await HandleRangeRequest(talk, blobStorage, httpContext, rangeHeader, cancellationToken);
        }

        var blob = await blobStorage.DownloadAsync("dharma-talks", talk.AudioBlobName, cancellationToken);
        if (blob == null)
            return Results.NotFound();

        return Results.File(blob.Content, blob.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> HandleRangeRequest(
        Domain.Entities.DharmaTalk talk,
        IBlobStorageService blobStorage,
        HttpContext httpContext,
        string rangeHeader,
        CancellationToken cancellationToken)
    {
        var totalSize = talk.AudioSizeBytes;

        if (!TryParseRange(rangeHeader, totalSize, out var start, out var end))
            return Results.StatusCode(416);

        var length = end - start + 1;

        var stream = await blobStorage.OpenReadAsync("dharma-talks", talk.AudioBlobName, start, length, cancellationToken);
        if (stream == null)
            return Results.NotFound();

        httpContext.Response.StatusCode = 206;
        httpContext.Response.Headers.ContentRange = $"bytes {start}-{end}/{totalSize}";
        httpContext.Response.Headers.AcceptRanges = "bytes";
        httpContext.Response.ContentType = talk.AudioContentType;
        httpContext.Response.ContentLength = length;

        await stream.CopyToAsync(httpContext.Response.Body, cancellationToken);

        return Results.Empty;
    }

    private static bool TryParseRange(string rangeHeader, long totalSize, out long start, out long end)
    {
        start = 0;
        end = totalSize - 1;

        if (!rangeHeader.StartsWith("bytes="))
            return false;

        var range = rangeHeader["bytes=".Length..];
        var parts = range.Split('-');

        if (parts.Length != 2)
            return false;

        if (!string.IsNullOrEmpty(parts[0]))
        {
            if (!long.TryParse(parts[0], out start))
                return false;
        }

        if (!string.IsNullOrEmpty(parts[1]))
        {
            if (!long.TryParse(parts[1], out end))
                return false;
        }
        else
        {
            end = totalSize - 1;
        }

        return start <= end && start < totalSize;
    }
}
