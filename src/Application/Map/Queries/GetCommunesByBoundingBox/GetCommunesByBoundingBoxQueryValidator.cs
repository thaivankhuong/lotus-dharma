using FluentValidation;

namespace LotusDharma.Application.Map.Queries.GetCommunesByBoundingBox;

public class GetCommunesByBoundingBoxQueryValidator : AbstractValidator<GetCommunesByBoundingBoxQuery>
{
    public GetCommunesByBoundingBoxQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 3000);

        RuleFor(x => x.Simplify)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(x => x.Xmax > x.Xmin && x.Ymax > x.Ymin)
            .WithMessage("Bounding box coordinates must define a positive envelope.");
    }
}


