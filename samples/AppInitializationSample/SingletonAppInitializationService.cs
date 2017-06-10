using AspNetCoreExt.AppInitialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace AppInitializationSample
{
    public class SingletonAppInitializationService : IAppInitializationService
    {
        private readonly ILogger logger;

        public SingletonAppInitializationService(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<SingletonAppInitializationService>();
        }

        public async Task Initialize()
        {
            this.logger.LogInformation("Simulation 5 seconds of asynchronous work");
            await Task.Delay(5000);

            this.logger.LogInformation("Simulation 5 seconds of synchronous work");
            Thread.Sleep(5000);
        }
    }
}
