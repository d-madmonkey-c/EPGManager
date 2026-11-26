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
			//configStore.Logger.LogInformation("Saving config update: {Request}", JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
			var config = configStore.SourceConfig;

			// M3U URLs
			var m3uUrls = request.M3uUrls;
			foreach(M3uSource source in m3uUrls)
			{
				// Generate a unique ID for this source
				if(string.IsNullOrEmpty(source.Id)) source.Id = Guid.NewGuid().ToString();
				if (config.M3uUrls.Any(mu => mu.Id == source.Id))
				{
					var target = config.M3uUrls.First(mu => mu.Id == source.Id);
					target.Priority = source.Priority;
					target.Name = source.Name;
					target.Url = source.Url;
				}
				else
				{
					config.M3uUrls.Add(source);
				}
			}
			config.M3uUrls.RemoveAll(old => !m3uUrls.Any(mu => mu.Id == old.Id));

			// EPG URLs
			var epgUrls = request.EpgUrls;
			foreach (EpgSource source in epgUrls)
			{
				// Generate a unique ID for this source
				if (string.IsNullOrEmpty(source.Id)) source.Id = Guid.NewGuid().ToString();
				if (config.EpgUrls.Any(eu => eu.Id == source.Id))
				{
					var target = config.EpgUrls.First(eu => eu.Id == source.Id);
					target.Priority = source.Priority;
					target.Offset = source.Offset;
					target.Name = source.Name;
					target.Url = source.Url;
				}
				else
				{
					config.EpgUrls.Add(source);
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