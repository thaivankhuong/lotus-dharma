using FluentValidation;

namespace LotusDharma.Application.Communes.Queries.GetCommuneById;

public class GetCommuneByIdQueryValidator : AbstractValidator<GetCommuneByIdQuery>
{
    public GetCommuneByIdQueryValidator()
    {
        RuleFor(x => x.CommuneId)
            .NotEmpty();

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Simplify.HasValue);
    }
}


