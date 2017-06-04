using AspNetCoreExt.AppInitialization.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization
{
    public class AppInitializationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IPlaceholderProvider placeholderProvider;
        private readonly ILogger logger;
        private readonly AppInitializationOptions options;

        private object syncRoot = new object();
        private Task initializationTask;
        private bool isInitialized;

        public AppInitializationMiddleware(RequestDelegate next,
            IOptions<AppInitializationOptions> options,
            ILoggerFactory loggerFactory,
            IPlaceholderProvider placeholderProvider,
            IHttpApplication<HttpContext> server)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.placeholderProvider = placeholderProvider ?? throw new ArgumentNullException(nameof(placeholderProvider));
            this.logger = loggerFactory?.CreateLogger<AppInitializationMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Invoke(HttpContext httpContext)
        {
            if (!this.isInitialized)
            {
                return Initialize(httpContext);
            }
            else
            {
                this.logger.LogDebug("Skipping initialization");

                return this.next(httpContext);
            }
        }

        private async Task Initialize(HttpContext httpContext)
        {
            this.logger.LogDebug("Testing if initialization has been kicked off");

            if (this.initializationTask == null)
            {
                lock (this.syncRoot)
                {
                    if (this.initializationTask == null)
                    {
                        this.initializationTask = PerformInitialize(httpContext);
                    }
                }
            }

            if (this.initializationTask.IsCompleted)
            { 
                // allow initialzation to serve up any exceptions
                await this.initializationTask;
            }

            this.logger.LogInformation("Serving a placeholder while waiting for the app to be initialized");
            await this.placeholderProvider.ServePlaceholderFile(httpContext);
        }

        private async Task PerformInitialize(HttpContext httpContext)
        {
            this.logger.LogInformation("Performing first time initialization");

            // resolve providers dynamically so we can release collect them once initialization has completed
            var initializationProviders = httpContext.RequestServices.GetServices<IAppInitializationProvider>();
            var initializationTasks = new List<Task>();
            foreach (var provider in initializationProviders)
            {
                var initializationTask = provider.Initialize();
                initializationTasks.Add(initializationTask);
            }

            try
            {
                await Task.WhenAll(initializationTasks);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                else
                {
                    throw;
                }
            }

            // unset kept references so that they can be collected
            initializationTasks = null;
            initializationProviders = null;

            this.logger.LogInformation("First time initialization performed");
            this.isInitialized = true;
        }
    }
}
