using LotusDharma.Application.Common.Exceptions;
using LotusDharma.Application.Products.Commands.CreateProduct;
using LotusDharma.Application.Products.Commands.DeleteProduct;
using LotusDharma.Application.Products.Commands.UpdateProduct;
using LotusDharma.Application.Products.Queries.GetProductById;
using LotusDharma.Application.Products.Queries.GetProducts;
using LotusDharma.Application.Products.Queries.GetProductsWithCategory;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LotusDharma.Web.Endpoints;

[Authorize]
public class Products : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetProducts).RequireAuthorization();
        groupBuilder.MapGet(GetProductsWithCategory, "with-category").RequireAuthorization();
        groupBuilder.MapGet(GetProductById, "{id}").RequireAuthorization();
        groupBuilder.MapPost(CreateProduct).RequireAuthorization();
        groupBuilder.MapPut(UpdateProduct, "{id}").RequireAuthorization();
        groupBuilder.MapDelete(DeleteProduct, "{id}").RequireAuthorization();
    }

    // GET /api/products
    public async Task<Ok<List<ProductDto>>> GetProducts(ISender sender)
    {
        var result = await sender.Send(new GetProductsQuery());
        return TypedResults.Ok(result);
    }

    // GET /api/products/with-category?categoryId=1&pageNumber=1&pageSize=20
    public async Task<Ok<List<ProductWithCategoryDto>>> GetProductsWithCategory(
        ISender sender,
        [AsParameters] GetProductsWithCategoryQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    // GET /api/products/{id}
    public async Task<Results<Ok<ProductDto>, NotFound>> GetProductById(ISender sender, int id)
    {
        try
        {
            var result = await sender.Send(new GetProductByIdQuery(id));
            return TypedResults.Ok(result);
        }
        catch (NotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    // POST /api/products
    public async Task<Created<int>> CreateProduct(ISender sender, CreateProductCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Products)}/{id}", id);
    }

    // PUT /api/products/{id}
    public async Task<Results<NoContent, BadRequest>> UpdateProduct(ISender sender, int id, UpdateProductCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // DELETE /api/products/{id}
    public async Task<NoContent> DeleteProduct(ISender sender, int id)
    {
        await sender.Send(new DeleteProductCommand(id));
        return TypedResults.NoContent();
    }
}
