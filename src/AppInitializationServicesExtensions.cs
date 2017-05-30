using AspNetCoreExt.AppInitialization.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization
{
    public static class AppInitializationServicesExtensions
    {
		public static IServiceCollection AddAppInitialization(this IServiceCollection services, Action<AppInitializationOptions> setupAction = null)
		{
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IPlaceholderProvider, PlaceholderProvider>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return services;
		}
	}
}
