using FluentValidation;

namespace LotusDharma.Application.Identity.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password là bắt buộc.")
            .MinimumLength(6).WithMessage("Password phải có ít nhất 6 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Password phải có ít nhất một chữ cái viết hoa.")
            .Matches(@"[a-z]").WithMessage("Password phải có ít nhất một chữ cái viết thường.")
            .Matches(@"[0-9]").WithMessage("Password phải có ít nhất một chữ số.")
            .Matches(@"[\!\?\*\.\@\#\$\%\^\&\+\=]").WithMessage("Password phải có ít nhất một ký tự đặc biệt (!?*.@#$%^&+=).");
    }
}

