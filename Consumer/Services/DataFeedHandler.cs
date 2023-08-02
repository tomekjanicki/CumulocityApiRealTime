using System.Text;
using Consumer.RealTime.Services;
using Microsoft.Extensions.Logging;

namespace Consumer.Services;

public sealed class DataFeedHandler : IDataFeedHandler
{
    private readonly ILogger<DataFeedHandler> _logger;

    public DataFeedHandler(ILogger<DataFeedHandler> logger) => 
        _logger = logger;

    public Task Handle(byte[] data, CancellationToken cancellationToken)
    {
        var dataAsString = Encoding.UTF8.GetString(data);
        _logger.LogInformation("Result: {Result}", dataAsString);

        return Task.CompletedTask;
    }
}