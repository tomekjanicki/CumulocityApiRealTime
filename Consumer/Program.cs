using Consumer;
using Consumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("appSettings.json"))
    .ConfigureServices(static (context, services) => ConfigureIoC.ConfigureServices(context, services))
    .Build();

await host.StartAsync(CancellationToken.None).ConfigureAwait(false);
var wrapper = host.Services.GetRequiredService<RealTimeExecutor>();
await wrapper.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);