using FluentValidation;

namespace LotusDharma.Application.Provinces.Queries.GetProvinces;

public class GetProvincesQueryValidator : AbstractValidator<GetProvincesQuery>
{
    public GetProvincesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 1000);

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Simplify.HasValue);
    }
}



