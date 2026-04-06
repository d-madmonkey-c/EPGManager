using EPGManager.Data;

namespace EPGManager.Endpoints;

public static class RefreshEndpoints
{
	public static void Map(WebApplication app)
	{
		app.MapPost("/refresh", async (
			Processor processor,
			ILogger<Program> logger,
			ConfigStore configStore,
			CacheStore cacheStore) =>
		{
			logger.LogInformation("/refresh: Refreshing M3U.");
			await processor.RefreshM3uAsync();
			logger.LogInformation("/refresh: Refreshing EPGs.");
			await processor.RefreshEpgAsync();
			logger.LogInformation("/refresh: Generating outputs.");
			await processor.GenerateOutputs();
			processor.LastRefresh = DateTime.Now;
			logger.LogInformation("/refresh completed.");
			var results = new
			{
				success = true,
				totalChannelCount = cacheStore.Channels.Count,
				newChannelCount = cacheStore.NewChannelIds.Count,
				selectedChannelCount = configStore.SelectedChannels.Count,
				totalSources = configStore.SourceConfig.EpgUrls.Count,
				totalEpgChannels = cacheStore.EpgChannels.Values.Sum(channels => channels?.Count ?? 0),
				totalEpgProgrammes = cacheStore.EpgProgrammes.Values.Sum(programmes => programmes?.Count ?? 0),
				status = $"Status: Refreshed {processor.LastRefresh:G}"
		};
			return Results.Ok(results);
		});

		app.MapPost("/refresh-m3u", async (
			Processor processor,
			CacheStore cacheStore) =>
		{
			try
			{
				await processor.RefreshM3uAsync();
				var newChannelCount = cacheStore.NewChannelIds.Count;
				return Results.Ok(new { success = true, message = $"M3U refreshed. Found {newChannelCount} new channels." });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, error = ex.Message });
			}
		});

		app.MapPost("/refresh-epg", async (
			Processor processor,
			CacheStore cacheStore) =>
		{
			try
			{
				await processor.RefreshEpgAsync();
				return Results.Ok(new { success = true, message = $"EPG refreshed. Found {cacheStore.EpgChannels.Count} available channel IDs." });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, error = ex.Message });
			}
		});

		app.MapPost("/refresh/epg/{id}", async (
			Processor processor,
			CacheStore cacheStore,
			string id) =>
		{
			try
			{
				await processor.RefreshEpgAsync(id);
				return Results.Ok(new { success = true, message = $"EPG refreshed. Found {cacheStore.EpgChannels.Count} available channel IDs." });
			}
			catch (Exception ex)
			{
				return Results.BadRequest(new { success = false, error = ex.Message });
			}
		});
	}
}