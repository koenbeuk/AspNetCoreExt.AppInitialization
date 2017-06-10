using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace AspNetCoreExt.AppInitialization.Internal
{
    internal class WarmupService : IWarmupService
    {
        private readonly ILogger<AppInitializationMiddleware> logger;
        private readonly AppInitializationOptions options;

        public WarmupService(IOptions<AppInitializationOptions> options, ILoggerFactory loggerFactory)
        {
            this.options = options.Value;
            this.logger = loggerFactory.CreateLogger<AppInitializationMiddleware>();
        }

        public bool IsWarmupRequest(HttpContext httpContext) => httpContext.Request.Headers.ContainsKey(this.options.WarmupRequestHeaderName);

        public Task RunWarmupRequests(Uri baseAddress)
        {
            using (var httpClient = new HttpClient()
            {
                BaseAddress = baseAddress
            })
            {
                httpClient.DefaultRequestHeaders.Add(this.options.WarmupRequestHeaderName, DateTime.UtcNow.ToString());

                IEnumerable<PathString> warmupRequestPaths;
                if (this.options.WarmupPaths?.Any() == true)
                {
                    warmupRequestPaths = this.options.WarmupPaths;
                }
                else
                {
                    warmupRequestPaths = Enumerable.Repeat(PathString.Empty, 1); // hit the root of the server by default
                }

                var warmupRequestTasks = RunWarmupRequests(httpClient, warmupRequestPaths);

                return Task.WhenAll(warmupRequestTasks);
            }
        }

        private IEnumerable<Task> RunWarmupRequests(HttpClient httpClient, IEnumerable<PathString> paths)
        {
            foreach (var path in paths)
            {
                yield return RunWarmupRequest(httpClient, path);
            }
        }


        private async Task RunWarmupRequest(HttpClient httpClient, PathString path)
        {
            var response = await httpClient.GetAsync(path);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                this.logger.LogWarning($"Warmup request for {{{nameof(path)}}} failed with: {{{nameof(statusCode)}}}", path, statusCode);
            }
        }
    }
}
