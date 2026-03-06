using System.Text.Json;
using LotusDharma.Domain.Common;
using LotusDharma.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.BackgroundJobs;

public class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxRetries = 3;

    public OutboxProcessorJob(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var domainEventType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == message.Type);

                if (domainEventType == null)
                {
                    _logger.LogWarning("Unknown domain event type: {Type}", message.Type);
                    message.Error = $"Unknown type: {message.Type}";
                    message.ProcessedOn = DateTimeOffset.UtcNow;
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, domainEventType) as BaseEvent;
                if (domainEvent != null)
                {
                    await mediator.Publish(domainEvent, cancellationToken);
                }

                message.ProcessedOn = DateTimeOffset.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to process outbox message {Id}, attempt {Retry}", message.Id, message.RetryCount);
            }
        }

        if (messages.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
