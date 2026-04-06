using EPGManager;
using EPGManager.Data;
using EPGManager.Endpoints;

namespace EPGManager;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<ConfigStore>();
        builder.Services.AddSingleton<CacheStore>();
        builder.Services.AddSingleton<OutputStore>();
        builder.Services.AddSingleton<Processor>();
        builder.Services.AddSingleton<RefreshWorker>();
        builder.Services.AddHostedService<RefreshWorker>();

        // Add endpoint modules
        //builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        // Run startup initialization
        Startup.Initialize(app);

        // Map endpoints
        ConfigEndpoints.Map(app);
        RefreshEndpoints.Map(app);
        OutputEndpoints.Map(app);
        EpgEndpoints.Map(app);

        app.UseStaticFiles();
        app.Run();
    }
}
