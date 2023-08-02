using Consumer.RealTime.Models;
using Consumer.RealTime.Services;
using Microsoft.Extensions.Logging;

namespace Consumer.Services;

public sealed class RealTimeExecutor
{
    private readonly IRealTimeWebSocketClient _client;
    private readonly ILogger<RealTimeExecutor> _logger;

    public RealTimeExecutor(IRealTimeWebSocketClient client, ILogger<RealTimeExecutor> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start executing.");
        try
        {
            await ExecuteInt(cancellationToken).ConfigureAwait(false);
            await ExecuteInt(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Generic exception.");
        }
        _logger.LogInformation("Finish executing.");
    }

    private async Task ExecuteInt(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Trying to connect.");
        var connect = await _client.Connect(cancellationToken).ConfigureAwait(false);
        await HandleConnect(connect, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleConnect(Error? connect, CancellationToken cancellationToken)
    {
        if (connect is not null)
        {
            _logger.LogError("Error Connect: {Error}", connect);

            return;
        }
        _logger.LogInformation("Connected.");
        await HandleSubscribe(cancellationToken).ConfigureAwait(false);
        await _client.Disconnect(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Disconnected.");
        Console.ReadLine();
    }

    private async Task HandleSubscribe(CancellationToken cancellationToken)
    {
        const string id1 = "28218078";
        const string id2 = "28229998";
        var subscription1 = new Subscription(id1, NotificationType.ManagedObject);
        var subscription2 = new Subscription(id2, NotificationType.ManagedObject);
        var subscribe1Task = _client.Subscribe(subscription1, cancellationToken);
        var subscribe2Task = _client.Subscribe(subscription2, cancellationToken);
        var subscribe1 = await subscribe1Task.ConfigureAwait(false);
        if (subscribe1 is not null)
        {
            _logger.LogError("Error Subscribe1: {Error}", subscribe1);
        }
        var subscribe2 = await subscribe2Task.ConfigureAwait(false);
        if (subscribe2 is not null)
        {
            _logger.LogError("Error Subscribe2: {Error}", subscribe2);
        }
        if (subscribe1 is null || subscribe2 is null)
        {
            if (subscribe1 is null)
            {
                _logger.LogInformation("Subscribed 1.");
            }
            if (subscribe2 is null)
            {
                _logger.LogInformation("Subscribed 2.");
            }
            Console.ReadLine();
            await HandleUnsubscribe(subscription1, subscription2, cancellationToken).ConfigureAwait(false);

            return;
        }

        Console.ReadLine();
    }

    private async Task HandleUnsubscribe(Subscription subscription1, Subscription subscription2, CancellationToken cancellationToken)
    {
        var unsubscribe1Task = _client.Unsubscribe(subscription1, cancellationToken);
        var unsubscribe2Task = _client.Unsubscribe(subscription2, cancellationToken);
        var unsubscribe1 = await unsubscribe1Task.ConfigureAwait(false);
        if (unsubscribe1 is not null)
        {
            _logger.LogError("Error Unsubscribe1: {Error}", unsubscribe1);
        }
        var unsubscribe2 = await unsubscribe2Task.ConfigureAwait(false);
        if (unsubscribe2 is not null)
        {
            _logger.LogError("Error Unsubscribe2: {Error}", unsubscribe2);
        }
        if (unsubscribe1 is null || unsubscribe2 is null)
        {
            if (unsubscribe1 is null)
            {
                _logger.LogInformation("Unsubscribed 1.");
            }
            if (unsubscribe2 is null)
            {
                _logger.LogInformation("Unsubscribed 2.");
            }
            Console.ReadLine();

            return;
        }

        Console.ReadLine();
    }
}