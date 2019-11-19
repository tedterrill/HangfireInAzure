using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace HangfireInAzure {
    public class HangfireJobActivator : JobActivator {
        private readonly IServiceProvider _serviceProvider;

        public HangfireJobActivator(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type) {
            var instance = _serviceProvider.GetService(type);

            if (instance == null && type.GetInterfaces().Any())
                instance = _serviceProvider.GetService(type.GetInterfaces().FirstOrDefault());

            return instance;
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context) {
            return new ServiceProviderScope(_serviceProvider, context);
        }
    }

    internal class ServiceProviderScope : JobActivatorScope {
        private readonly JobActivatorContext _context;
        private readonly IServiceScope       _scope;

        public ServiceProviderScope(IServiceProvider serviceProvider, JobActivatorContext context) {
            _context = context;
            _scope   = serviceProvider.CreateScope();
        }

        public override object Resolve(Type type) {
            var instance = _scope.ServiceProvider.GetService(type);

            if (instance == null && type.GetInterfaces().Any())
                instance = _scope.ServiceProvider.GetService(type.GetInterfaces().FirstOrDefault());

            return instance;
        }

        public override void DisposeScope() {
            _scope?.Dispose();
        }
    }
}
