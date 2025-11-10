using LotusDharma.Application.Common.Interfaces;

namespace LotusDharma.Application.Identity.Commands.LoginUser;

public record LoginUserCommand : IRequest<LoginResult?>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResult?>
{
    private readonly IIdentityService _identityService;

    public LoginUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResult?> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.LoginAsync(request.Email, request.Password);
    }
}

