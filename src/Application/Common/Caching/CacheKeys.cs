namespace LotusDharma.Application.Common.Caching;

/// <summary>
/// Centralized cache key definitions for consistency.
/// Pattern: {entity}:{id/action}:{optional-filters}
/// </summary>
public static class CacheKeys
{
    // Categories
    public static string AllCategories => "categories:all";
    public static string CategoryById(int id) => $"categories:{id}";
    public static string ActiveCategories => "categories:active";

    // Products  
    public static string AllProducts => "products:all";
    public static string ProductById(int id) => $"products:{id}";
    public static string ProductsByCategory(int categoryId) => $"products:category:{categoryId}";
    public static string ProductsByCategoryPaged(int categoryId, int page, int pageSize) 
        => $"products:category:{categoryId}:page:{page}:size:{pageSize}";
    public static string ProductsWithCategory(int? categoryId, int page, int pageSize)
        => categoryId.HasValue 
            ? $"products:with-category:{categoryId}:page:{page}:size:{pageSize}"
            : $"products:with-category:all:page:{page}:size:{pageSize}";

    // TodoLists & TodoItems (existing entities)
    public static string AllTodoLists => "todolists:all";
    public static string TodoListById(int id) => $"todolists:{id}";
    public static string TodoItemsByList(int listId) => $"todoitems:list:{listId}";

    // Patterns for bulk operations
    public static class Patterns
    {
        public const string AllCategories = "categories:*";
        public const string AllProducts = "products:*";
        public const string ProductsByCategory = "products:category:*";
        public const string AllTodoLists = "todolists:*";
        public const string AllTodoItems = "todoitems:*";
    }
}

