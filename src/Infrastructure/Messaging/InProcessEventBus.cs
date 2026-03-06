using LotusDharma.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Messaging;

/// <summary>
/// In-process event bus using MediatR. Replace with Azure Service Bus or RabbitMQ
/// via MassTransit when scaling to distributed microservices.
/// </summary>
public class InProcessEventBus : IEventBus
{
    private readonly IMediator _mediator;
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(IMediator mediator, ILogger<InProcessEventBus> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogInformation("Publishing event: {EventType}", typeof(T).Name);

        if (@event is INotification notification)
        {
            await _mediator.Publish(notification, cancellationToken);
        }
    }
}
