using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using EPGManager.Data;
using Microsoft.AspNetCore.Mvc;

namespace EPGManager.Endpoints;

public static class ConfigEndpoints
{
	public static void Map(WebApplication app)
	{
		app.MapGet("/", async (
			HttpResponse res,
			Processor processor) =>
		{
			/*↕►▼→←Ξ‡⁞≡*/
			// Buiild final HTML
			string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
			string html = File.ReadAllText(templatePath);
			var styleHash = Utility.ComputeFileHash(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "styles.css"));
			var scriptHash = Utility.ComputeFileHash(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "scripts.js"));
			html = string.Format(html, styleHash, scriptHash, $"Status: Refreshed {processor.LastRefresh:G}", processor.GenerateSourceConfigContent(), processor.GenerateChannelConfigContent(), processor.GeneratePreviewContent(), "");

			res.ContentType = "text/html; charset=utf-8";
			await res.WriteAsync(html);
		});

		app.MapPost("/config", async (ConfigUpdate request, ConfigStore configStore, CacheStore cacheStore) =>
		{
			var config = configStore.SourceConfig;

			// M3U URL
			config.M3uUrl = request.PrimaryUrl;

			// EPG URLs
			var epgUrls = request.EpgUrls;
			for (int i = 0; i < epgUrls.Count; i++)
			{
				// Generate a unique ID for this source
				var epgUrl = epgUrls[i];
				epgUrl.Id = string.IsNullOrEmpty(epgUrl.Id) ? $"user_{DateTime.UtcNow.Ticks}_{i}" : epgUrl.Id;
				if (config.EpgUrls.Any(eu => eu.Id == epgUrl.Id))
				{
					var target = config.EpgUrls.First(eu => eu.Id == epgUrl.Id);
					target.Priority = epgUrl.Priority;
					target.Offset = epgUrl.Offset;
					target.Name = epgUrl.Name;
					target.Url = epgUrl.Url;
				}
				else
				{
					config.EpgUrls.Add(epgUrl);
				}
			}
			config.EpgUrls.RemoveAll(old => !epgUrls.Any(eu => eu.Id == old.Id));

			// Process selected channels
			var selectedChannels = request.SelectedChannels;
			if (selectedChannels != null)
			{
				configStore.SelectedChannels.Clear();
				configStore.SelectedChannels.AddRange(selectedChannels);
			}

			//await configStore.SaveAsync(config);
			configStore.SaveAll();
			//await refreshWorker.TriggerRefreshAsync();

			cacheStore.ReviewFeedback.Clear();
			cacheStore.ReviewFeedback.AddRange(request.ReviewFeedback);
			cacheStore.SaveAll();

			return Results.Ok(new { success = true, message = "Config updated." });
		});

		app.MapGet("/sourceConfigContent", async (Processor processor) =>
		{
			return processor.GenerateSourceConfigContent();
		});

		app.MapGet("/channelConfigContent", async (Processor processor) =>
		{
			return processor.GenerateChannelConfigContent();
		});

		app.MapGet("/previewContent", async (Processor processor) =>
		{
			return processor.GeneratePreviewContent();
		});

		app.MapGet("/reviewContent", async (Processor processor) =>
		{
			return processor.GenerateReviewContent();
		});
	}
}