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
        private readonly IWarmupService warmupService;
        private readonly ILogger logger;
        private readonly AppInitializationOptions options;

        private object syncRoot = new object();
        private Task initializationTask;
        private bool isInitialized;

        public AppInitializationMiddleware(RequestDelegate next,
            IOptions<AppInitializationOptions> options,
            ILoggerFactory loggerFactory,
            IPlaceholderProvider placeholderProvider,
            IWarmupService warmupService)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.placeholderProvider = placeholderProvider ?? throw new ArgumentNullException(nameof(placeholderProvider));
            this.warmupService = warmupService ?? throw new ArgumentNullException(nameof(warmupService));
            this.logger = loggerFactory?.CreateLogger<AppInitializationMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!this.isInitialized)
            {
                bool served;
                if (this.warmupService.IsWarmupRequest(httpContext))
                {
                    served = false;
                    this.logger.LogDebug("Detected a warmup request, skipping initialization");
                }
                else
                {
                    served = await Initialize(httpContext);
                }
                if (served)
                {
                    return;
                }
            }
            else
            {
                this.logger.LogDebug("Skipping initialization");
            }

            await this.next(httpContext);
            
        }

        private async Task<bool> Initialize(HttpContext httpContext)
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
                this.isInitialized = true;

                return false;
            }
            else
            {
                this.logger.LogInformation("Serving a placeholder while waiting for the app to be initialized");
                await this.placeholderProvider.ServePlaceholderFile(httpContext);

                return true;
            }
        }

        private async Task PerformInitialize(HttpContext httpContext)
        {
            this.logger.LogInformation("Performing first time initialization");

            var warmupUrls = GetWarmupUrls(httpContext).ToArray();

            // resolve providers dynamically so we can release collect them once initialization has completed
            var initializationProviders = httpContext.RequestServices.GetServices<IAppInitializationService>();
            var initializationTasks = initializationProviders.Select(x => x.Initialize());

            try
            {
                await Task.WhenAll(initializationTasks);
                await this.warmupService.RunWarmupRequests(warmupUrls);
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
        }

        private IEnumerable<Uri> GetWarmupUrls(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host.Host}:{request.Host.Port}";

            var uriBuilder = new UriBuilder(request.Scheme, request.Host.Host);
            if (request.Host.Port != null)
            {
                uriBuilder.Port = request.Host.Port.Value;
            }

            foreach (var path in this.options.WarmupPaths)
            {
                uriBuilder.Path = request.Path;
                yield return uriBuilder.Uri;
            }
        }
    }
}
