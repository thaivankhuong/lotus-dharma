using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Identity.Commands.LoginUser;
using LotusDharma.Application.Identity.Commands.RegisterUser;
using LotusDharma.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LotusDharma.Web.Endpoints;

public class Identity : EndpointGroupBase
{
    public override string? GroupName => "Users";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register").AllowAnonymous();
        groupBuilder.MapPost(Login, "login").AllowAnonymous();
    }

    public async Task<Results<Ok<object>, BadRequest<object>>> Register(ISender sender, RegisterUserCommand command)
    {
        var result = await sender.Send(command);

        if (!result.Succeeded)
        {
            return TypedResults.BadRequest<object>(new
            {
                message = "Đăng ký thất bại",
                errors = result.Errors
            });
        }

        return TypedResults.Ok<object>(new
        {
            message = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ."
        });
    }

    public async Task<Results<Ok<LoginResult>, UnauthorizedHttpResult>> Login(ISender sender, LoginUserCommand command)
    {
        var result = await sender.Send(command);

        if (result == null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(result);
    }
}

