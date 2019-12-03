using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace HangfireInAzure {
    public class MyLongRunningJob {
        private readonly ILogger<MyLongRunningJob> _logger;

        public MyLongRunningJob(ILogger<MyLongRunningJob> logger) {
            _logger = logger;
        }

        public void DoWork(string work, CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                _logger.LogDebug($"[{DateTime.Now:s}] Long running task is doing work: {work}");
                Thread.Sleep(TimeSpan.FromSeconds(12));
            }

            _logger.LogWarning("Long running task was cancelled.");
        }
    }
}