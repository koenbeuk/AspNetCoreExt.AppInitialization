using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCoreExt.AppInitialization.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task PerformsInitialization()
        {
            var appInitializationProvider = new FakeAppInitializationProvider();
            var builder = new WebHostBuilder()
                .ConfigureServices(app =>
                {
                    app.Add(new ServiceDescriptor(typeof(IAppInitializationProvider), appInitializationProvider));
                    app.AddAppInitialization();
                })
                .Configure(app =>
                {
                    app.UseAppInitialization();
                    app.Run(context =>
                    {
                        context.Response.StatusCode = 200;
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var firstResponse = await client.GetStringAsync("");

            const string refreshResponseHeader = @"<meta http-equiv=""refresh"" content=""5"">";

            Assert.True(appInitializationProvider.Initialized);
            Assert.Contains(refreshResponseHeader, firstResponse);

            var secondResponse = await client.GetStringAsync("");

            Assert.True(appInitializationProvider.Initialized);
            Assert.DoesNotContain(refreshResponseHeader, secondResponse);
        }

        [Fact]
        public async Task ServersConfiguredFileFromWebRoot()
        {
            var appInitializationProvider = new FakeAppInitializationProvider();
            var builder = new WebHostBuilder()
                .ConfigureServices(app =>
                {
                    app.Add(new ServiceDescriptor(typeof(IAppInitializationProvider), appInitializationProvider));
                    app.AddAppInitialization();
                })
                .Configure(app =>
                {
                    app.UseAppInitialization();
                    app.Run(context =>
                    {
                        context.Response.StatusCode = 200;
                        return Task.FromResult(0);
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var firstResponse = await client.GetStringAsync("");

            const string refreshResponseHeader = @"<meta http-equiv=""refresh"" content=""5"">";

            Assert.True(appInitializationProvider.Initialized);
            Assert.Contains(refreshResponseHeader, firstResponse);

            var secondResponse = await client.GetStringAsync("");

            Assert.True(appInitializationProvider.Initialized);
            Assert.DoesNotContain(refreshResponseHeader, secondResponse);
        }
    }
}
