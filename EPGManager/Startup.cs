//using EPGManager;
using EPGManager.Data;

namespace EPGManager;

public static class Startup
{
	public static void Initialize(WebApplication app)
	{
		var configStore = app.Services.GetRequiredService<ConfigStore>();
		configStore.LoadAll();

		var cacheStore = app.Services.GetRequiredService<CacheStore>();
		cacheStore.LoadAll(configStore.SourceConfig.EpgUrls.Select(u => u.Id).ToList());

		var outputStore = app.Services.GetRequiredService<OutputStore>();
		var processor = app.Services.GetRequiredService<Processor>();
		processor.LastRefresh = outputStore.LoadEpg();
	}
}
