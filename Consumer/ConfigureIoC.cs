using System.Net.Http.Headers;
using Consumer.RealTime.Services;
using Consumer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
        services.AddHttpClient(Constants.ClientName, static (provider, client) => ConfigureClient(provider, client));
        services.AddSingleton<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IRealTimeWebSocketClient2, RealTimeWebSocketClient2>();
        services.AddSingleton<Notification2Facade>();
    }

    private static void ConfigureClient(IServiceProvider provider, HttpClient client)
    {
        var settings = provider.GetRequiredService<IOptions<ConfigurationSettings>>().Value;
        client.BaseAddress = new Uri(settings.HttpUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Helper.GetEncodedUserNameAndPassword(settings.UserName, settings.Password));
    }
}