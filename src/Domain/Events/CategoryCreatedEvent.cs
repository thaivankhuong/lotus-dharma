namespace LotusDharma.Domain.Events;

public class CategoryCreatedEvent : BaseEvent
{
    public CategoryCreatedEvent(Category category)
    {
        Category = category;
    }

    public Category Category { get; }
}

