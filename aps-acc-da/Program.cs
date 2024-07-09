using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class Program
{
    // Entry point of the application
    public static void Main(string[] args)
    {
        // Build and run the host
        CreateHostBuilder(args).Build().Run();
    }

    // Configures and returns an IHostBuilder instance
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)

        // Add configuration settings from appsettings.user.json and environment variables
        .ConfigureAppConfiguration(configureDelegate: (context, config) =>
        {
            config.AddJsonFile("appsettings.user.json", optional: false, reloadOnChange: true);
            config.AddEnvironmentVariables();
        })

        // Add required DA services to the dependency injection container
        .ConfigureServices((hostContext, services) =>
        {
            services.AddDesignAutomation(hostContext.Configuration);
        })

        // Configure web host defaults
        .ConfigureWebHostDefaults(webBuilder =>
        {
            // Specify the startup class for configuring the application
            webBuilder.UseStartup<Startup>();
        });

    // Add the following to the appsettings.user.json file
    /*
    {
        "APS_CLIENT_ID": "",
        "APS_CLIENT_SECRET": "",
        "APS_CALLBACK_URL": "http://localhost:8080/api/auth/callback",
        "Forge": {
            "ClientId": "",
            "ClientSecret": ""
        }
    }
    */
}

