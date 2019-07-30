using System;
using System.Diagnostics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Diagnostics.RuntimeHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var timer = new Stopwatch();
            timer.Start();
            var result = WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .Build();
            timer.Stop();
            Console.WriteLine($"App Built in {timer.ElapsedMilliseconds / 1000} seconds");

            return result;
        }
    }
}
