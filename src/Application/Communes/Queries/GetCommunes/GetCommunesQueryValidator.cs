using FluentValidation;

namespace LotusDharma.Application.Communes.Queries.GetCommunes;

public class GetCommunesQueryValidator : AbstractValidator<GetCommunesQuery>
{
    public GetCommunesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 1500);

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Simplify.HasValue);
    }
}



