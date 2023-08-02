using Consumer.RealTime.Services;
using Consumer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Consumer;

public static class ConfigureIoC
{
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddOptions<ConfigurationSettings>().Bind(context.Configuration.GetSection(ConfigurationSettings.Section));
        services.AddSingleton<IRealTimeWebSocketClient, RealTimeWebSocketClient>();
        services.AddSingleton<RealTimeExecutor>();
        services.AddSingleton<IDataFeedHandler, DataFeedHandler>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IClientWebSocketWrapperFactory, ClientWebSocketWrapperFactory>();
    }
}