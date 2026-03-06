using LotusDharma.Domain.Entities;

namespace LotusDharma.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }
    
    DbSet<Product> Products { get; }
    
    DbSet<Category> Categories { get; }

    DbSet<Province> Provinces { get; }

    DbSet<Commune> Communes { get; }

    // Identity
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserToken> UserTokens { get; }

    // Media
    DbSet<Speaker> Speakers { get; }
    DbSet<DharmaTalkSeries> DharmaTalkSeries { get; }
    DbSet<DharmaTalk> DharmaTalks { get; }

    // Outbox
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
