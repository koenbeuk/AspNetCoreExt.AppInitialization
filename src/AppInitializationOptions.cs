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
    }
}
