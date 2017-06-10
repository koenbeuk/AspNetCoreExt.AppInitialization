using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreExt.AppInitialization.Tests
{
    public class FakeAppInitializationService : IAppInitializationService
    {
        public Task Initialize()
        {
            this.Initialized = true;
            return Task.FromResult(0);
        }

        public bool Initialized { get; set; }
    }
}
