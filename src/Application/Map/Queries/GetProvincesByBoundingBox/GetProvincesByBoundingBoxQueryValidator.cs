using FluentValidation;

namespace LotusDharma.Application.Map.Queries.GetProvincesByBoundingBox;

public class GetProvincesByBoundingBoxQueryValidator : AbstractValidator<GetProvincesByBoundingBoxQuery>
{
    public GetProvincesByBoundingBoxQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 2000);

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.Xmax > x.Xmin && x.Ymax > x.Ymin)
            .WithMessage("Bounding box coordinates must define a positive envelope.");
    }
}












