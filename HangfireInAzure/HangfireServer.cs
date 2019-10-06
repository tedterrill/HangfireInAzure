using Hangfire;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HangfireInAzure {
    public class HangfireServer {
        private static void ConfigureServices(IServiceCollection services) {
            services.AddLogging(loggingBuilder => {
                        loggingBuilder.AddConsole();
                    })
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    // register our job activator
                    .AddSingleton<JobActivator>(s => new HangfireJobActivator(s))
                    // register tasks/jobs here
                    .AddScoped<MyTask>()
                ;
        }

        [NoAutomaticTrigger]
        [FunctionName(nameof(RunServer))]
        public async Task RunServer(ILogger           logger,
                                    CancellationToken cancellationToken) {
            try {
                // register logging and dependencies
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                // create service provider
                var serviceProvider =
                    serviceCollection.BuildServiceProvider(new ServiceProviderOptions {
                        ValidateOnBuild = true
                    });

                // set up our storage method - this demo uses SQL server
                logger.LogInformation("Configuring Hangfire...");
                GlobalConfiguration.Configuration.UseSqlServerStorage("Hangfire");
                GlobalConfiguration.Configuration.UseActivator(serviceProvider.GetService<JobActivator>());

                var server = new BackgroundJobServer(new BackgroundJobServerOptions {
                    WorkerCount               = 1,
                    HeartbeatInterval         = TimeSpan.FromSeconds(10),
                    SchedulePollingInterval   = TimeSpan.FromSeconds(10),
                    CancellationCheckInterval = TimeSpan.FromSeconds(10)
                });
                logger.LogInformation("The service has been started.");

                cancellationToken.Register(() => {
                    logger.LogInformation("The token has been cancelled. Disposing the hangfire server.");
                    server.Dispose();
                });

                await RunAsync(logger, serviceProvider, cancellationToken);
            }
            catch (OperationCanceledException oce) {
                logger.LogWarning($"Cancellation was raised while running server: {oce.Message}", oce);
                throw;
            }
            catch (Exception ex) {
                logger.LogError($"Error occurred while running server: {ex.Message}", ex);
                throw;
            }
        }

        private static async Task RunAsync(ILogger logger, IServiceProvider serviceProvider, CancellationToken cancellationToken) {
            logger.LogInformation("The Hangfire server was started. We'll let you know every 30 seconds that it's still running.");
            
            while (!cancellationToken.IsCancellationRequested) {
                logger.LogInformation($"[{DateTime.Now:s}] Server is running...");
                RunSomeJobs(serviceProvider);

                await Task.Delay(30000, cancellationToken);
            }

            logger.LogInformation("Stopping the heartbeat.");
        }

        private static void RunSomeJobs(IServiceProvider serviceProvider) {
            var task = serviceProvider.GetService<MyTask>();

            BackgroundJob.Enqueue(() => task.DoWork("Doing some work right away", CancellationToken.None)); // use 'None' and hangfire will inject a cancellation token

            var delay = TimeSpan.FromSeconds(10);
            BackgroundJob.Schedule(() => task.DoWork($"Schedule some work to be done at {DateTime.Now.Add(delay)}", CancellationToken.None),
                                   delay);
        }
    }
}
