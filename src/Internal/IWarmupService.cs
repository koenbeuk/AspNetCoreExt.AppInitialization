using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization.Internal
{
    public interface IWarmupService
    {
        bool IsWarmupRequest(HttpContext httpContext);

		Task RunWarmupRequests(Uri baseAddress); 
    }
}
