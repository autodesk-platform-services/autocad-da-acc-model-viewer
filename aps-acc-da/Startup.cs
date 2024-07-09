using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // Configures the services used by the application.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add MVC controllers
        services.AddControllers();

        // Retrieve APS (Autodesk Platform Services) configuration from appsettings
        var clientID = Configuration["APS_CLIENT_ID"];
        var clientSecret = Configuration["APS_CLIENT_SECRET"];
        var callbackURL = Configuration["APS_CALLBACK_URL"];
        // Bucket key is optional; a transient bucket will be created if not provided
        string? bucket = Configuration["APS_BUCKET_KEY"];

        // Validate required APS configuration
        if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(callbackURL))
        {
            throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
        }

        // Register APS service with dependency injection
        services.AddSingleton<APS>(new APS(clientID, clientSecret, bucket ?? string.Empty, callbackURL));

        // Add SignalR with Newtonsoft.Json protocol configuration
        services.AddSignalR().AddNewtonsoftJsonProtocol(opt =>
        {
            // Ignore reference loops in JSON serialization
            opt.PayloadSerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        });
    }

    // Configures the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Enable detailed error page in development environment
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Serve default files (e.g., index.html) and static files (e.g., CSS, JavaScript)
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Enable routing
        app.UseRouting();

        // Configure endpoints
        app.UseEndpoints(endpoints =>
        {
            // Map SignalR hub for Design Automation
            endpoints.MapHub<DAController.DesignAutomationHub>("/api/signalr/designautomation");

            // Map controllers for API endpoints
            endpoints.MapControllers();
        });
    }
}

