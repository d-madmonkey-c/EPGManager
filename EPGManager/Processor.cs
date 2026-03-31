using System;
using System.IO.Compression;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class Processor
{
	private readonly ILogger<Processor> _logger;
	private readonly ConfigStore _configStore;
	private readonly OutputStore _outputStore;
	private readonly CacheStore _cacheStore;
	private readonly HttpClient _httpClient = new();

	public Processor(ILogger<Processor> logger, ConfigStore configStore, OutputStore outputStore, CacheStore cacheStore)
	{
		_logger = logger;
		_configStore = configStore;
		_outputStore = outputStore;
		_cacheStore = cacheStore;
	}

	/// <summary>
	/// Refreshes the M3U cache and identifies new channels
	/// </summary>
	public async Task RefreshM3uAsync(CancellationToken ct = default)
	{
		try
		{
			//_configStore.LoadAll();
			var config = _configStore.SourceConfig;
			//var config = await _configStore.LoadAsync();

			if (string.IsNullOrWhiteSpace(config.M3uUrl))
			{
				_logger.LogWarning("Primary M3U URL not configured.");
				return;
			}

			var currentIds = new List<string>(_cacheStore.Channels.Select(c => c.Id));

			// Download and cache M3U content
			var m3uText = await DownloadText(config.M3uUrl, ct);
			//_cacheStore.SaveM3uCacheAsync(m3uText);

			// Parse channels
			var channels = M3uParser.Parse(m3uText);
			_cacheStore.Channels = channels;

			/*
			// Update secondary source info for these channels
			var secondarySources = new List<EpgSource>();

			if (secondarySources.Any())
			{
				var secondaryDocs = new List<XDocument>();
				foreach (var source in secondarySources)
				{
					var cachedDoc = await _cacheStore.LoadEpgCacheAsync(source.Name);
					secondaryDocs.Add(cachedDoc ?? await DownloadAndParseGzipXml(source.Url, ct));
				}
				//ChannelMapper.AttachSecondaryIds(channels, secondaryDocs, secondarySources);

				// Try to find EPG matches for selected channels
				foreach (var selectedChannel in _configStore.SelectedChannels)
				{
					var m3uChannel = channels.FirstOrDefault(c => c.Id == selectedChannel.Id);
					if (m3uChannel != null)
					{
						// Copy auto-detected IDs to selected channel config
						// foreach (var kvp in m3uChannel.SecondaryChannelIds)
						// {
						// 	if (!selectedChannel.EpgChannelIds.ContainsKey(kvp.Key))
						// 	{
						// 		selectedChannel.EpgChannelIds[kvp.Key] = kvp.Value;
						// 	}
						// }
					}
				}
			}
			*/

			// Identify new channels
			var newIds = channels.Select(c => c.Id).Except(currentIds).ToList();
			_cacheStore.NewChannelIds = newIds;

			// Update output store
			//_outputStore.AvailableChannels = channels;
			//_cacheStore.Channels = channels;
			//_outputStore.NewChannelIds = new HashSet<string>(newIds);

			// Update config to track current channels
			//config.PreviousCachedChannelIds = currentIds.ToList();
			//await _configStore.SaveAsync(config);

			_cacheStore.SaveAll();
			_logger.LogInformation($"M3U refresh completed. Found {channels.Count} channels, {newIds.Count} new.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error refreshing M3U");
			throw;
		}
	}

	/// <summary>
	/// Refreshes EPG data from secondary sources
	/// </summary>
	public async Task RefreshEpgAsync(CancellationToken ct = default)
	{
		try
		{
			var config = _configStore.SourceConfig;

			// var secondarySources = new List<EpgSource>();
			// secondarySources.AddRange(config.EpgUrls);

			// var allChannelIds = new HashSet<string>();

			foreach (EpgSource source in config.EpgUrls)
			{
				var doc = await DownloadAndParseGzipXml(source.Url, ct);
				//await _cacheStore.SaveEpgCacheAsync(source.Id, doc);
				var epgData = EpgParser.ParseEpgDoc(doc);
				_cacheStore.EpgChannels[source.Id] = epgData.Channels;
				_cacheStore.EpgProgrammes[source.Id] = epgData.Programmes;

				/*
				// Extract available channel IDs from this EPG
				var channelIds = doc.Root?
					.Elements("channel")
					.Select(ch => (string?)ch.Attribute("id"))
					.Where(id => !string.IsNullOrWhiteSpace(id))
					.Cast<string>()
					.ToList() ?? new List<string>();

				foreach (var id in channelIds)
					allChannelIds.Add(id);

				// Try to find matches for selected channels in this EPG
				var epgChannels = doc.Root?
					.Elements("channel")
					.Select(ch => new
					{
						Id = (string)ch.Attribute("id")!,
						Name = (string)ch.Element("display-name")!
					})
					.ToList() ?? new();
				*/

				/*  Channel matching logic moved to ChannelMapper to be shared between EPG and M3U refreshes
				foreach (var selectedChannel in _configStore.SelectedChannels)
				{
					if (source.Id != null && selectedChannel.EpgChannelIds.ContainsKey(source.Id))
						continue; // Already has a mapping for this source by id

					// Backward compatibility: if old key is stored by source name, migrate it
					if (!string.IsNullOrEmpty(source.Name) && selectedChannel.EpgChannelIds.ContainsKey(source.Name))
					{
						selectedChannel.EpgChannelIds[source.Id ?? source.Name] = selectedChannel.EpgChannelIds[source.Name];
						selectedChannel.EpgChannelIds.Remove(source.Name);
						continue;
					}

					// Try to find a match by existing EPG ID from other sources
					string? foundId = null;
					foreach (var otherSourceId in selectedChannel.EpgChannelIds.Keys)
					{
						if (selectedChannel.EpgChannelIds.TryGetValue(otherSourceId, out var otherEpgId))
						{
							// Look for the same EPG ID in this source
							if (epgChannels.Any(c => c.Id == otherEpgId))
							{
								foundId = otherEpgId;
								break;
							}
						}
					}

					// If not found by ID, try by name matching
					if (foundId == null)
					{
						var match = epgChannels.FirstOrDefault(c =>
							string.Equals(c.Id, selectedChannel.Id, StringComparison.OrdinalIgnoreCase) ||
							Normalize(c.Name) == Normalize(selectedChannel.Name));

						if (match != null)
						{
							foundId = match.Id;
						}
					}

					if (foundId != null)
					{
						selectedChannel.EpgChannelIds[source.Id ?? source.Name] = foundId;
					}
				}
				*/
			}

			// Update config with cached channel IDs
			//config.CachedEpgChannelIds = allChannelIds.ToList();
			//await _configStore.SaveAsync(config);
			_cacheStore.SaveAll();

			_logger.LogInformation($"EPG refresh completed. Found {_cacheStore.EpgChannels.Values.SelectMany(c => c).Distinct().Count()} available channel IDs across all sources.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error refreshing EPG");
			throw;
		}
	}

	public void GenerateOutputs()
	{
		_outputStore.M3u = M3uBuilder.Build(_configStore.SelectedChannels);
		_outputStore.Epg = EpgBuilder.Build(_configStore.SelectedChannels, _configStore.SourceConfig.EpgUrls, _cacheStore).ToString();
	}

	private async Task<string> DownloadText(string url, CancellationToken ct)
	{
		using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(ct);
	}

	private async Task<XDocument> DownloadAndParseGzipXml(string url, CancellationToken ct)
	{
		if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("ftp", StringComparison.OrdinalIgnoreCase))
		{
			using var response = await _httpClient.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();

			await using var stream = await response.Content.ReadAsStreamAsync(ct);
			await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
			return XDocument.Load(gzip);
		}
		else
		{
			// Assume local file
			using var fileStream = File.OpenRead(url);
			using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
			return XDocument.Load(gzip);
		}
	}
}
