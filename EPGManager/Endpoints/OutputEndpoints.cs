using System.Text;
using EPGManager.Data;

namespace EPGManager.Endpoints;

public static class OutputEndpoints
{
	public static void Map(WebApplication app)
	{
		app.MapGet("/output/m3u", async (
			OutputStore store,
			Processor processor) =>
		{
			var content = store.M3u;
			if (content == null)
			{
				await processor.GenerateOutputs();
				content = store.M3u;
				if (content == null)
					return Results.NotFound("Unable to load or generate content.");
			}

			return Results.File(
				Encoding.UTF8.GetBytes(content),
				"audio/x-mpegurl",
				"myguide.m3u"
			);
		});

		app.MapGet("/output/epg", async (
			OutputStore store,
			Processor processor) =>
		{
			if (store.Epg == null)
			{
				await processor.GenerateOutputs();
				if (store.Epg == null)
					return Results.NotFound("Unable to load or generate content.");
			}
			var path = store._epgGzPath;

			if (!File.Exists(path))
				return Results.NotFound("EPG file missing.");

			var stream = File.OpenRead(path);

			return Results.File(
				stream,
				"application/gzip",
				"myguide.xml.gz",
				enableRangeProcessing: true
			);
		});
	}
}
