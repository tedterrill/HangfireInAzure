using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HangfireInAzure {
    public class JobScheduler {
        public static async Task RunAsync(ILogger           logger,
                                          IServiceProvider  serviceProvider,
                                          CancellationToken cancellationToken) {

            RunLongRunningJob(serviceProvider);

            while (!cancellationToken.IsCancellationRequested) {
                RunSomeJobs(serviceProvider);
                await Task.Delay(30000, cancellationToken);
            }
        }

        private static void RunSomeJobs(IServiceProvider serviceProvider) {
            var job = serviceProvider.GetService<MySimpleJob>();

            // Queue up a job to run immediately
            BackgroundJob.Enqueue(() =>
                                      job.DoWork(
                                          "Doing some work right away",
                                          CancellationToken.None) // use 'None' and hangfire will inject a cancellation token
            );


            // Schedule a job to run 10 seconds later
            var delay = TimeSpan.FromSeconds(10);
            var runAt = new DateTimeOffset(DateTime.Now.Add(delay));
            BackgroundJob.Schedule(() =>
                                       job.DoWork(
                                           $"This work is being scheduled for {runAt}",
                                           CancellationToken.None),
                                   runAt
            );
        }

        private static void RunLongRunningJob(IServiceProvider serviceProvider) {
            var longRunningJob = serviceProvider.GetService<MyLongRunningJob>();

            BackgroundJob.Enqueue(() =>
                                      longRunningJob.DoWork(
                                          "Staying busy", CancellationToken.None)
            );
        }
    }
}