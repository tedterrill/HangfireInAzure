using System;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace HangfireInAzureTemp {
    public class MyTask {
        private readonly ILogger<MyTask> _logger;

        public MyTask(ILogger<MyTask> logger) {
            _logger = logger;
        }

        public void DoWork(string work, CancellationToken cancellationToken) {
            _logger.LogDebug($"[{DateTime.Now:s}] {work}");
        }
    }
}
