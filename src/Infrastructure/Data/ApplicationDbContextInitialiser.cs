using LotusDharma.Domain.Constants;
using LotusDharma.Domain.Entities;
using LotusDharma.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger, 
        ApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            var exists = await _context.Database.CanConnectAsync();
            
            if (!exists)
            {
                // Lần đầu tiên: Tạo database
                _logger.LogInformation("Database not found. Creating...");
                await _context.Database.MigrateAsync();
            }
            else
            {
                // Lần sau: Chỉ apply migrations mới
                var pending = await _context.Database.GetPendingMigrationsAsync();
                if (pending.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations...", pending.Count());
                    await _context.Database.MigrateAsync();
                }
                else
                {
                    _logger.LogInformation("✅ Database is up to date");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new Role 
        { 
            Name = Roles.Administrator,
            Description = "Administrator role với full quyền hạn"
        };

        if (!await _context.Roles.AnyAsync(r => r.Name == administratorRole.Name))
        {
            _context.Roles.Add(administratorRole);
            await _context.SaveChangesAsync();
        }
        else
        {
            administratorRole = await _context.Roles.FirstAsync(r => r.Name == Roles.Administrator);
        }

        // Default users
        var administrator = new User 
        { 
            UserName = "administrator@localhost", 
            Email = "administrator@localhost",
            EmailConfirmed = true,
            LockoutEnabled = false,
            PasswordHash = _passwordHasher.HashPassword("Administrator1!")
        };

        if (!await _context.Users.AnyAsync(u => u.UserName == administrator.UserName))
        {
            _context.Users.Add(administrator);
            await _context.SaveChangesAsync();

            // Gán role cho administrator
            var userRole = new UserRole
            {
                UserId = administrator.Id,
                RoleId = administratorRole.Id
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã tạo user Administrator: {Email} / Password: Administrator1!", administrator.Email);
        }

        // Default data
        // Seed, if necessary
        if (!await _context.TodoLists.AnyAsync())
        {
            _context.TodoLists.Add(new TodoList
            {
                Title = "Todo List Mẫu",
                Items =
                {
                    new TodoItem { Title = "Tạo todo list 📃" },
                    new TodoItem { Title = "Check off item đầu tiên ✅" },
                    new TodoItem { Title = "Nhận ra bạn đã hoàn thành 2 việc! 🤯"},
                    new TodoItem { Title = "Thưởng cho bản thân một giấc ngủ dài 🏆" },
                }
            });

            await _context.SaveChangesAsync();
        }
    }
}
