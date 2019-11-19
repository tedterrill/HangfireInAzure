using Microsoft.Azure.WebJobs.Host;
using System;

namespace HangfireInAzure {
    public class WebJobJobActivator : IJobActivator {
        private readonly IServiceProvider _service;

        public WebJobJobActivator(IServiceProvider service) {
            _service = service;
        }
        public T CreateInstance<T>() {
            return (T)_service.GetService(typeof(T));
        }
    }
}
