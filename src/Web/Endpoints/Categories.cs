using LotusDharma.Application.Categories.Commands.CreateCategory;
using LotusDharma.Application.Categories.Commands.DeleteCategory;
using LotusDharma.Application.Categories.Commands.UpdateCategory;
using LotusDharma.Application.Categories.Queries.GetCategories;
using LotusDharma.Application.Categories.Queries.GetCategoryById;
using LotusDharma.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LotusDharma.Web.Endpoints;

[Authorize]
public class Categories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCategories).RequireAuthorization();
        groupBuilder.MapGet(GetCategoryById, "{id}").RequireAuthorization();
        groupBuilder.MapPost(CreateCategory).RequireAuthorization();
        groupBuilder.MapPut(UpdateCategory, "{id}").RequireAuthorization();
        groupBuilder.MapDelete(DeleteCategory, "{id}").RequireAuthorization();
    }

    // GET /api/categories
    public async Task<Ok<List<CategoryDto>>> GetCategories(ISender sender)
    {
        var result = await sender.Send(new GetCategoriesQuery());
        return TypedResults.Ok(result);
    }

    // GET /api/categories/{id}
    public async Task<Results<Ok<CategoryDto>, NotFound>> GetCategoryById(ISender sender, int id)
    {
        try
        {
            var result = await sender.Send(new GetCategoryByIdQuery(id));
            return TypedResults.Ok(result);
        }
        catch (NotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    // POST /api/categories
    public async Task<Created<int>> CreateCategory(ISender sender, CreateCategoryCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Categories)}/{id}", id);
    }

    // PUT /api/categories/{id}
    public async Task<Results<NoContent, BadRequest>> UpdateCategory(ISender sender, int id, UpdateCategoryCommand command)
    {
        if (id != command.Id)
            return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // DELETE /api/categories/{id}
    public async Task<NoContent> DeleteCategory(ISender sender, int id)
    {
        await sender.Send(new DeleteCategoryCommand(id));
        return TypedResults.NoContent();
    }
}

