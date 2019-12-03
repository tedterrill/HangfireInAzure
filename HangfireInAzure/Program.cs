using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HangfireInAzure {
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static async Task Main() {
            var hostBuilder = new HostBuilder()
                              .ConfigureWebJobs(webJobsBuilder => {
                                  webJobsBuilder.AddAzureStorageCoreServices();
                                  webJobsBuilder.AddAzureStorage();
                              })
                              .ConfigureLogging((context, loggingBuilder) => {
                                  // add your logging here - we'll use a simple console logger for this demo
                                  loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                                  loggingBuilder.AddConsole();
                              })
                              .ConfigureServices((context, services) => {
                                  services
                                      .AddSingleton(context.Configuration);
                              })
                              .UseConsoleLifetime();

            using (var host = hostBuilder.Build()) {
                var logger = host.Services.GetService<ILogger<Program>>();

                try {
#if DEBUG
                    Environment.SetEnvironmentVariable("WEBJOBS_SHUTDOWN_FILE", "c:\\temp\\WebJobsShutdown");
#endif
                    
                    var watcher           = new WebJobsShutdownWatcher();
                    var cancellationToken = watcher.Token;

                    logger.LogInformation("Starting the host");
                    await host.StartAsync(cancellationToken);
                    var jobHost = host.Services.GetService<IJobHost>();

                    logger.LogInformation("Starting the Hangfire server");
                    await jobHost
                          .CallAsync(nameof(HangfireServer.RunServer),
                                     cancellationToken: cancellationToken)
                          .ContinueWith(result => {
                              logger.LogInformation(
                                  $"The job host stopped with state: {result.Status}");
                          }, cancellationToken);
                }
                catch (Exception ex) {
                    logger.LogError(ex.Message);
                }
            }
        }
    }
}