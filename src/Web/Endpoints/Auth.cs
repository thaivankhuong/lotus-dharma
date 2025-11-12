using LotusDharma.Application.Identity.Commands.LoginWithGoogle;
using LotusDharma.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using MediatR;

namespace LotusDharma.Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override string? GroupName => "auth";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GoogleLogin, "google").AllowAnonymous();
    }

    public async Task<IResult> GoogleLogin(ISender sender, LoginWithGoogleCommand command)
    {
        var result = await sender.Send(command);

        if (result == null)
            return Results.BadRequest(new { message = "Invalid Google id token." });

        return Results.Ok(result);
    }
}


