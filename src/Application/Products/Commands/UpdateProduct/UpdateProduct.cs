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
    // UpdatedIdUser sẽ tự động lấy từ JWT claims (không cần truyền từ client)
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
    
    // Handle method ý nghĩa:
    // - Xử lý lệnh cập nhật thông tin sản phẩm (UpdateProductCommand)
    // - Tìm sản phẩm theo Id, nếu không thấy thì throw not found exception
    // - Cập nhật các trường: Name, Description, Price, Stock, CategoryId, IsActive
    // - Tự động gán trường UpdatedIdUser = UserId từ JWT claims (không cần client truyền lên)
    // - Ghi nhận thay đổi vào database (SaveChangesAsync)

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
        entity.UpdatedIdUser = _user.Id;  // Tự động lấy UserId từ JWT claims
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
