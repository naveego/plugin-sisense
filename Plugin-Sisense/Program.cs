using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using Plugin_Sisense.API.Replication;
using Plugin_Sisense.Helper;


namespace Plugin_Sisense
{
    public class Program
    {
        public static Server Server;
        public static void Main(string[] args)
        {
            try
            {
                // Add final chance exception handler
                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    Logger.Error(null, $"died: {eventArgs.ExceptionObject}");
                };
                
                // clean old logs on start up
                Logger.Clean();

                // wait to exit until closed
                CreateWebHostBuilder(args).Build().Run();

                Logger.Info("Plugin exiting...");

                // shutdown server
                Server.ShutdownAsync().Wait();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
        }
        
        public static void Run()
        {
            // create new server and start it
            Server = new Grpc.Core.Server
            {
                Services = { Publisher.BindService(new Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            Server.Start();
    
            // write out the connection information for the Hashicorp plugin runner
            var output = String.Format("{0}|{1}|{2}|{3}:{4}|{5}",
                1, 1, "tcp", "localhost", Server.Ports.First().BoundPort, "grpc");
            
            Console.WriteLine(output);
            
            Logger.Info("Started on port " + Server.Ports.First().BoundPort);

            try
            {
                // create the config file for Sisense and restart Sisense
                var sisenseConfig = Replication.GenerateSisenseConfig();
                Replication.AddSisenseService(sisenseConfig);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://127.0.0.1:0")
                .UseStartup<Startup>();
    }
}