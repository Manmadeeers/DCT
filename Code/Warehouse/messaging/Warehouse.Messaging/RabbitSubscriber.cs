using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Warehouse.Messaging;

public abstract class RabbitSubscriber(
    IOptions<RabbitOptions> options,
    ILogger logger) : BackgroundService
{
    protected abstract string QueueName { get; }

    protected abstract Task HandleAsync(
        EventEnvelope message,
        CancellationToken ct);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Subscriber disconnected from {Queue}. Retrying in 5 seconds.",
                    QueueName);

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        var factory = options.Value.CreateFactory();

        await using var connection =
            await factory.CreateConnectionAsync(ct);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: ct);

        var disconnected = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        connection.ConnectionShutdownAsync += (_, _) =>
        {
            disconnected.TrySetResult(true);
            return Task.CompletedTask;
        };

        channel.ChannelShutdownAsync += (_, _) =>
        {
            disconnected.TrySetResult(true);
            return Task.CompletedTask;
        };

        await RabbitTopology.DeclareAsync(channel, ct);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.UnregisteredAsync += (_, _) =>
        {
            disconnected.TrySetResult(true);
            return Task.CompletedTask;
        };

        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var processed = false;

            try
            {
                var message =
                    JsonSerializer.Deserialize<EventEnvelope>(
                        delivery.Body.Span,
                        EventJson.Options)
                    ?? throw new JsonException("The event is null.");

                if (message.EventId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(message.EventType) ||
                    message.Version != 1 ||
                    message.Data.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException(
                        "Invalid event envelope or unsupported version.");
                }

                logger.LogInformation(
                    "Received {EventType}, EventId={EventId}, Redelivered={Redelivered}",
                    message.EventType,
                    message.EventId,
                    delivery.Redelivered);

                await HandleWithRetryAsync(message, ct);

                processed = true;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {

                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Processing failed on {Queue}. Sending delivery to dead-letter queue.",
                    QueueName);
            }

            try
            {
                if (processed)
                {
                    await channel.BasicAckAsync(
                        deliveryTag: delivery.DeliveryTag,
                        multiple: false,
                        cancellationToken: ct);
                }
                else
                {
                    await channel.BasicRejectAsync(
                        deliveryTag: delivery.DeliveryTag,
                        requeue: false,
                        cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                disconnected.TrySetException(exception);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        logger.LogInformation(
            "Subscriber is listening to {Queue}",
            QueueName);

        await disconnected.Task.WaitAsync(ct);

        throw new IOException(
            $"RabbitMQ connection or consumer closed for {QueueName}.");
    }

    private async Task HandleWithRetryAsync(
        EventEnvelope message,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await HandleAsync(message, ct);
                return;
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < 3)
            {
                logger.LogWarning(
                    exception,
                    "Attempt {Attempt} failed for EventId={EventId}",
                    attempt,
                    message.EventId);

                await Task.Delay(
                    TimeSpan.FromSeconds(attempt),
                    ct);
            }
        }
    }
}