using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization
{
    public static class AppInitializationBuilderExtensions
    {
        public static IApplicationBuilder UseAppInitialization(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.UseMiddleware<AppInitializationMiddleware>();

            return builder;
        }
    }
}
