using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Entities;
using LotusDharma.Domain.Events.Products;

namespace LotusDharma.Application.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int CategoryId { get; init; }
    // CreatedIdUser sẽ tự động lấy từ JWT claims (không cần truyền từ client)
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateProductCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            CreatedIdUser = _user.Id,  // Tự động lấy UserId từ JWT claims
            IsActive = true
        };

        entity.AddDomainEvent(new ProductCreatedEvent(entity));

        _context.Products.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
