using System.Reflection;
using System.Reflection.Emit;
using LotusDharma.Application.Common.Interfaces;
using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LotusDharma.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Domain entities
    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Province> Provinces => Set<Province>();

    public DbSet<Commune> Communes => Set<Commune>();

    // Custom Identity entities
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<UserToken> UserTokens => Set<UserToken>();

    // Media
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<DharmaTalkSeries> DharmaTalkSeries => Set<DharmaTalkSeries>();
    public DbSet<DharmaTalk> DharmaTalks => Set<DharmaTalk>();

    // Outbox
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("pg_trgm");
        builder.HasPostgresExtension("unaccent");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
