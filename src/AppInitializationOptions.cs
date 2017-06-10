using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization
{
    public class AppInitializationOptions
    {
		/// <summary>
		/// A path to a file that gets served while the app is booting up
		/// </summary>
		public PathString StartupPlaceholderFilePath { get; set; }

		/// <summary>
		/// todo
		/// </summary>
		public TimeSpan? RefreshTime { get; set; }

        /// <summary>
        /// The name of a header that gets appended to each warmup request
        /// </summary>
        public string WarmupRequestHeaderName { get; set; } = "X-ASPNETCORE-WARMUP";

        /// <summary>
        /// A sequence of additional paths that will get called as part of the warming up phase
        /// </summary>
        public ICollection<PathString> WarmupPaths { get; set; } = new List<PathString>();
    }
}
