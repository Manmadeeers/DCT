using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Warehouse.Messaging;

namespace Warehouse.Api.Infrastructure;

public sealed class RabbitPublisherWorker(
    WarehouseEventBuffer buffer,
    IOptions<RabbitOptions> options,
    ILogger<RabbitPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        PendingEvent? pending = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = options.Value.CreateFactory();

                await using var connection =
                    await factory.CreateConnectionAsync(stoppingToken);

                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);

                await using var channel =
                    await connection.CreateChannelAsync(
                        options: channelOptions,
                        cancellationToken: stoppingToken);

                await RabbitTopology.DeclareAsync(
                    channel,
                    stoppingToken);

                logger.LogInformation(
                    "RabbitMQ publisher connected");

                while (!stoppingToken.IsCancellationRequested)
                {
                    pending ??= await buffer.Reader.ReadAsync(
                        stoppingToken);

                    var body = JsonSerializer.SerializeToUtf8Bytes(
                        pending.Envelope,
                        EventJson.Options);

                    var properties = new BasicProperties
                    {
                        ContentType = "application/json",
                        Persistent = true,
                        MessageId = pending.Envelope.EventId.ToString(),
                        Type = pending.Envelope.EventType
                    };

                    using var publishTimeout =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            stoppingToken);

                    publishTimeout.CancelAfter(
                        TimeSpan.FromSeconds(10));

                    await channel.BasicPublishAsync(
                        exchange: RabbitTopology.Exchange,
                        routingKey: pending.RoutingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: body,
                        cancellationToken: publishTimeout.Token);

                    logger.LogInformation(
                        "Published {EventType}, EventId={EventId}, RoutingKey={RoutingKey}",
                        pending.Envelope.EventType,
                        pending.Envelope.EventId,
                        pending.RoutingKey);

                    pending = null;
                }
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
                    "RabbitMQ publishing failed. Retrying in 5 seconds.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }
    }
}