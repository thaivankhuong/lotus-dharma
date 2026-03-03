using LotusDharma.Application.Common.Interfaces;

namespace LotusDharma.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public int CategoryId { get; init; }
    public bool IsActive { get; init; }
    // UpdatedIdUser will be automatically taken from JWT claims (no need to pass from client)
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateProductCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }
    
    // Handle method purpose:
    // - Process product update command (UpdateProductCommand)
    // - Find product by Id, throw not found exception if not found
    // - Update fields: Name, Description, Price, Stock, CategoryId, IsActive
    // - Automatically assign UpdatedIdUser = UserId from JWT claims
    // - Save changes to database (SaveChangesAsync)

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Products
            .FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.Stock = request.Stock;
        entity.CategoryId = request.CategoryId;
        entity.UpdatedIdUser = _user.Id;  // Automatically get UserId from JWT claims
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
