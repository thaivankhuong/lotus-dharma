using FluentValidation;

namespace LotusDharma.Application.Identity.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(v => v.IdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}
