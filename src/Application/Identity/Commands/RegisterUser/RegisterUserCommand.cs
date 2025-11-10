using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Application.Common.Models;

namespace LotusDharma.Application.Identity.Commands.RegisterUser;

public record RegisterUserCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IIdentityService _identityService;

    public RegisterUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        return result;
    }
}

