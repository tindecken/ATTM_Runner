using Coravel;
using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Runner.Invocables;
using System;
using System.IO;
using System.Reflection;

namespace Runner
{
    class Program
    {
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            host.Services.UseScheduler(scheduler =>
            {
                // Remind schedule to repeat the same job in every five-second   
                scheduler
                    .Schedule<AutoRunner>()
                    .EveryFiveSeconds()
                    .PreventOverlapping("AutoRunner");
            });
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddScheduler();
                    // register job with container  
                    services.AddTransient<AutoRunner>();
                });
    }
}
