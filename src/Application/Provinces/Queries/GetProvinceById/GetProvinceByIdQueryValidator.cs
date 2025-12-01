using FluentValidation;

namespace LotusDharma.Application.Provinces.Queries.GetProvinceById;

public class GetProvinceByIdQueryValidator : AbstractValidator<GetProvinceByIdQuery>
{
    public GetProvinceByIdQueryValidator()
    {
        RuleFor(x => x.ProvinceId)
            .GreaterThan(0);

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Simplify.HasValue);
    }
}



