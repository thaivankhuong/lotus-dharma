namespace LotusDharma.Domain.Events;

public class CategoryDeletedEvent : BaseEvent
{
    public CategoryDeletedEvent(Category category)
    {
        Category = category;
    }

    public Category Category { get; }
}

