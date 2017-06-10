using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

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

        public async Task RunWarmupRequests(IEnumerable<Uri> urls)
        {
            if (this.options.WarmupPaths?.Any() != true)
            {
                return;
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(this.options.WarmupRequestHeaderName, DateTime.UtcNow.ToString());

                var warmupRequestTasks = urls.Select(x => RunWarmupRequest(httpClient, x));

                await Task.WhenAll(warmupRequestTasks);
            }
        }

        private async Task RunWarmupRequest(HttpClient httpClient, Uri url)
        {
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                this.logger.LogWarning($"Warmup request for {{{nameof(url)}}} failed with: {{{nameof(statusCode)}}}", url, statusCode);
            }
        }
    }
}
