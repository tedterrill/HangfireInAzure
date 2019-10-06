using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace HangfireInAzure {
    public class MySimpleJob {
        private readonly ILogger<MySimpleJob> _logger;

        public MySimpleJob(ILogger<MySimpleJob> logger) {
            _logger = logger;
        }

        public void DoWork(string work, CancellationToken cancellationToken) {
            _logger.LogDebug($"[{DateTime.Now:s}] {work}");
        }
    }
}