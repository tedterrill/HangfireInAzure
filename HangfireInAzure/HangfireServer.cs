﻿using Hangfire;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HangfireInAzure {
    public class HangfireServer {
        private static void ConfigureServices(IServiceCollection services) {
            services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); })
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    // register our job activator
                    .AddSingleton<JobActivator>(s => new HangfireJobActivator(s))
                    // register tasks/jobs here
                    .AddScoped<MySimpleJob>()
                    .AddScoped<MyLongRunningJob>()
                ;
        }

        [NoAutomaticTrigger]
        [FunctionName(nameof(RunServer))]
        public async Task RunServer(ILogger logger,
                                    CancellationToken cancellationToken) {
            try {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                // create service provider
                var serviceProvider =
                    serviceCollection.BuildServiceProvider(new ServiceProviderOptions {
                        ValidateOnBuild = true
                    });

                logger.LogInformation("Configuring Hangfire...");

                GlobalConfiguration.Configuration
                                   // set up our storage method - this demo uses SQL server
                                   .UseSqlServerStorage("Hangfire")
                                   // get the job activator
                                   .UseActivator(serviceProvider.GetService<JobActivator>())
                    ;

                var server = new BackgroundJobServer(new BackgroundJobServerOptions {
                    WorkerCount = 2,
                    HeartbeatInterval = TimeSpan.FromSeconds(10),
                    SchedulePollingInterval = TimeSpan.FromSeconds(10),
                    CancellationCheckInterval = TimeSpan.FromSeconds(10)
                });
                logger.LogInformation("The service has been started.");

                cancellationToken.Register(() => {
                    logger.LogInformation("The token has been cancelled. Disposing the hangfire server.");
                    server.Dispose();
                    logger.LogInformation("We'll give all the tasks 30 more seconds to gracefully handle this.");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                });

                logger.LogInformation(
                    "The Hangfire server has started. We'll let you know every 30 seconds that it's still running.");

                // Put some code here to add jobs to the queue. This is just for testing. You will not do this in a real app
                JobScheduler.RunAsync(logger, serviceProvider, cancellationToken);

                while (!cancellationToken.IsCancellationRequested) {
                    logger.LogInformation($"[{DateTime.Now:s}] Server is running...");
                    await Task.Delay(30000, cancellationToken);
                }

                logger.LogInformation("Stopping the heartbeat.");
            }
            catch (OperationCanceledException oce) {
                logger.LogWarning($"Cancellation was raised while running server: {oce.Message}", oce);
            }
            catch (Exception ex) {
                logger.LogError($"Error occurred while running server: {ex.Message}", ex);
                throw;
            }
        }
    }
}