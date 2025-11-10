using FluentValidation;

namespace LotusDharma.Application.Identity.Commands.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password là bắt buộc.");
    }
}

