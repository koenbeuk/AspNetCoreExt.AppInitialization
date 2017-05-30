using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCoreExt.AppInitialization.Internal
{
    internal class PlaceholderProvider: IPlaceholderProvider
    {
        const string refreshHeaderName = "Refresh";

        private readonly AppInitializationOptions options;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly ILogger logger;

        private TimeSpan GetRefreshInterval() => this.options.RefreshTime ?? TimeSpan.FromSeconds(5);
        private string GetRefreshUrl(HttpContext httpContext)
        {
            var request = httpContext.Request;
            return UriHelper.BuildRelative(request.PathBase, request.Path, request.QueryString);
        }


        public PlaceholderProvider(IOptions<AppInitializationOptions> appInitializationOptions, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
        {
            this.options = appInitializationOptions?.Value ?? new AppInitializationOptions { };
            this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            this.logger = loggerFactory.CreateLogger<AppInitializationMiddleware>();
        }

        public async Task ServePlaceholderFile(HttpContext httpContext)
        {
            var headers = httpContext.Response.GetTypedHeaders();
            headers.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.Zero,
                MustRevalidate = true
            };
            headers.ContentType = new MediaTypeHeaderValue("text/html")
            {
                Encoding = Encoding.UTF8
            };
            if (this.options.RefreshTime != null)
            {
                var refreshUrl = GetRefreshUrl(httpContext);
                var refreshInterval = this.options.RefreshTime.Value.TotalSeconds;
                var refreshValue = $"{refreshInterval};{refreshUrl}";
                headers.Append(refreshHeaderName, refreshValue);
            }

            var served = false;
            if (this.options.StartupPlaceholderFilePath.HasValue)
            {
                served = await ServeStaticFile(httpContext, this.options.StartupPlaceholderFilePath);
            }

            if (!served)
            {
                await ServeContent(httpContext);
            }
        }

        private Task ServeContent(HttpContext httpContext)
        {
            var refreshInterval = GetRefreshInterval();

            this.logger.LogDebug("Writing content placeholder");

            return httpContext.Response.WriteAsync($@"
<http>
    <head>
        <meta http-equiv=""refresh"" content=""{refreshInterval.TotalSeconds}"">
    </head>
    <body>
        We'll be online shortly...
    </body>
</http>");
        }

        protected virtual async Task<bool> ServeStaticFile(HttpContext httpContext, PathString filePath)
        {
            var filePathString = filePath.ToString();
            var fileInfo = this.hostingEnvironment.WebRootFileProvider.GetFileInfo(filePathString);
            if (!fileInfo.Exists)
            {
                this.logger.LogWarning("{filePath} could not be found, searched location: {webRootFolder}", this.hostingEnvironment.WebRootPath);
                return false;
            }

            await httpContext.Response.SendFileAsync(fileInfo);
            return true;
        }
    }
}
