using EPGManager.Data;

namespace EPGManager.Endpoints;

public static class EpgEndpoints
{
	public static void Map(WebApplication app)
	{
		app.MapGet("/epg/list-epgSources", async (
			ConfigStore configStore,
			CacheStore cacheStore) =>
		{
			var sources = configStore.SourceConfig.EpgUrls.OrderBy(s=>s.Priority).ThenBy(s=>s.Name).Select(
				s => new
				{
					id = s.Id,
					name = s.Name,
					channelCount = cacheStore.EpgChannels[s.Id].Count()
				}
			).ToList();
			return Results.Ok(sources);
		});

		app.MapGet("/epg/list-epgChannels/{sourceId}", async (
			ConfigStore configStore,
			CacheStore cacheStore,
			string sourceId) =>
		{
			if (!cacheStore.EpgChannels.ContainsKey(sourceId))
			{
				return Results.NotFound($"No EPG data found for source ID: {sourceId}");
			}
			var channels = cacheStore.EpgChannels[sourceId].Select(
				c => new
				{
					id = c.Id,
					name = c.Name
				}
			).OrderBy(c => c.name).ToList();
			return Results.Ok(channels);
		});

		/*
		app.MapGet("/api/epg-channels", async (
			ConfigStore configStore,
			CacheStore cacheStore) =>
		{
			//var config = await configStore.LoadAsync();
			var epgSources = new List<object>();

			foreach (var source in configStore.SourceConfig.EpgUrls)
			{
				try
				{
					var channels = new List<object>();

					// Try to load cached EPG data via ID or fallback to name-based cache
					XDocument? doc = null;
					if (!string.IsNullOrEmpty(source.Id))
					{
						doc = await cacheStore.LoadEpgCacheAsync(source.Id);
					}
					if (doc == null && !string.IsNullOrEmpty(source.Name))
					{
						doc = await cacheStore.LoadEpgCacheAsync(source.Name);
					}

					if (doc != null)
					{
						channels = doc.Root?
							.Elements("channel")
							.Select(ch => new
							{
								id = (string)ch.Attribute("id")!,
								name = (string)ch.Element("display-name")!
							})
							.Where(ch => !string.IsNullOrEmpty(ch.id) && !string.IsNullOrEmpty(ch.name))
							.OrderBy(ch => ch.name)
							.Cast<object>()
							.ToList() ?? new List<object>();
					}

					epgSources.Add(new
					{
						id = source.Id,
						name = source.Name,
						url = source.Url,
						channels = channels
					});
				}
				catch
				{
					// Still include sources even if they error
					epgSources.Add(new
					{
						id = source.Id,
						name = source.Name,
						url = source.Url,
						channels = new List<object>()
					}
					);
				}
			}
			return Results.Ok(epgSources);
		});
		*/
	}
}