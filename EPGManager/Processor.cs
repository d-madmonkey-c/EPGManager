using System;
using System.Data;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
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

	public DateTime LastRefresh = DateTime.UnixEpoch;

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
			var config = _configStore.SourceConfig;

			if (string.IsNullOrWhiteSpace(config.M3uUrl))
			{
				_logger.LogWarning("Primary M3U URL not configured.");
				return;
			}

			var currentIds = new List<string>(_cacheStore.Channels.Select(c => c.Id));

			// Download and cache M3U content
			var m3uText = await DownloadText(config.M3uUrl, ct);

			// Parse channels
			var channels = M3uParser.Parse(m3uText);
			_cacheStore.Channels = channels;

			// Update Urls for Selected Channels
			foreach (SelectedChannel selected in _configStore.SelectedChannels)
			{
				selected.Uri = channels.FirstOrDefault(c => c.Id == selected.Id)?.Uri ?? selected.Uri;
			}

			// Identify missing channels
			var missingIds = _configStore.SelectedChannels.Select(c => c.Id).Except(channels.Select(c => c.Id)).ToList();
			_cacheStore.MissingChannelIds = missingIds;

			// Identify new channels
			var newIds = channels.Select(c => c.Id).Except(currentIds).ToList();
			_cacheStore.NewChannelIds = newIds;

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

			foreach (EpgSource source in config.EpgUrls)
			{
				try
				{
					var doc = await DownloadAndParseGzipXml(source.Url, ct);
					//await _cacheStore.SaveEpgCacheAsync(source.Id, doc);
					var epgData = EpgParser.ParseEpgDoc(doc, source.Offset);
					_cacheStore.EpgChannels[source.Id] = epgData.Channels;
					_cacheStore.EpgProgrammes[source.Id] = epgData.Programmes;
				}
				catch (Exception ex)
				{
					_logger.LogError($"Failed processing EPG for {source.Name}.", ex);
				}
			}

			// Update config with cached channel IDs
			_cacheStore.SaveAll();

			_logger.LogInformation($"EPG refresh completed. Found {_cacheStore.EpgChannels.Values.SelectMany(c => c).Distinct().Count()} available channel IDs across all sources.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error refreshing EPG");
			throw;
		}
	}

	public async Task RefreshEpgAsync(string id, CancellationToken ct = default)
	{
		try
		{
			EpgSource source = _configStore.SourceConfig.EpgUrls.First(e => e.Id == id);
			var doc = await DownloadAndParseGzipXml(source.Url, ct);
			var epgData = EpgParser.ParseEpgDoc(doc, source.Offset);
			_cacheStore.EpgChannels[source.Id] = epgData.Channels;
			_cacheStore.EpgProgrammes[source.Id] = epgData.Programmes;
			_cacheStore.SaveAll();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed processing EPG for {id}.", id);
			throw;
		}
	}

	public async Task GenerateOutputs(CancellationToken ct = default)
	{
#pragma warning disable CA1873
		_logger.LogInformation("Generating M3U..");
		_outputStore.M3u = M3uBuilder.Build(_configStore.SelectedChannels);
		_logger.LogInformation("M3U Generated with {ChannelCount} channels.", _configStore.SelectedChannels.Count);
		_logger.LogInformation("Generating EPG...");
		_outputStore.Epg = EpgBuilder.Build(_configStore.SelectedChannels, _configStore.SourceConfig.EpgUrls, _cacheStore);
		_logger.LogInformation("Genereated EPG with {ProgrammeCount} programmes.", _outputStore.Epg.Root?.Elements("programme").Count() ?? 0);
#pragma warning restore CA1873
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

	internal string GenerateSourceConfigContent()
	{
		string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "epgurl.partial.html");
		string templateHtml = File.ReadAllText(templatePath);
		var urlListHtml = string.Join("", _configStore.SourceConfig.EpgUrls
			.OrderBy(eu => eu.Priority)
			.ThenBy(eu => eu.Name)
			.Select(eu => string.Format(templateHtml, eu.Id, eu.Priority, eu.Offset, eu.Name, eu.Url)));

		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sourceconfig.partial.html");
		templateHtml = File.ReadAllText(templatePath);
		var sourceConfigHtml = string.Format(templateHtml, _configStore.SourceConfig.M3uUrl, urlListHtml);

		return sourceConfigHtml;
	}

	internal string GenerateChannelConfigContent()
	{
		var channels = _cacheStore.Channels;
		var newChannelIds = _cacheStore.NewChannelIds ?? [];
		var missingChannelIds = _cacheStore.MissingChannelIds ?? [];
		var selectedChannels = _configStore.SelectedChannels;

		var grouped = channels
			.GroupBy(c => c.Group ?? "Ungrouped")
			.OrderBy(g => g.Key)
			.ToList();
		string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "availablegroup.partial.html");
		string groupHtml = File.ReadAllText(templatePath);
		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "availablechannel.partial.html");
		string channelHtml = File.ReadAllText(templatePath);
		var availableChannelsHtml = string.Join("", grouped.Select(g => string.Format(groupHtml, g.Key, string.Join("", g.Select(c =>
		{
			var channelItemHtml = string.Format(channelHtml, c.Id, c.Name, c.Group ?? "", c.LogoUri ?? "", c.Uri);
			if (newChannelIds.Contains(c.Id))
			{
				// Add new-channel class to the channel-item div
				channelItemHtml = channelItemHtml.Replace("class='channel-item'", "class='channel-item new-channel'");
			}
			else if (missingChannelIds.Contains(c.Id))
			{
				// Add new-channel class to the channel-item div
				channelItemHtml = channelItemHtml.Replace("class='channel-item'", "class='channel-item missing-channel'");
			}
			return channelItemHtml;
		})))));

		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "selectedchannel.partial.html");
		string templateHtml = File.ReadAllText(templatePath);

		var selectedChannelsHtml = string.Join("", selectedChannels.Select(c => string.Format(
			templateHtml,
			c.Id,
			c.Name,
			c.LogoUri,
			c.Uri,
			string.Join(", ", c.Groups),
			HtmlEncoder.Default.Encode(JsonSerializer.Serialize(c.EpgChannelIds.OrderBy(eci => _configStore.SourceConfig.EpgUrls.First(eu => eu.Id == eci.SourceId).Priority).Select(eci => new
			{
				SourceId = eci.SourceId,
				SourceName = _configStore.SourceConfig.EpgUrls.First(eu => eu.Id == eci.SourceId).Name,
				EpgId = eci.EpgId
			}
		))))));

		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "channelconfig.partial.html");
		templateHtml = File.ReadAllText(templatePath);
		var channelConfigHtml = string.Format(templateHtml, availableChannelsHtml, selectedChannelsHtml);

		return channelConfigHtml;
	}

	internal string GeneratePreviewContent()
	{
		DateTime startTime = DateTime.Today.AddHours(DateTime.Now.Hour);
		DateTime t1 = startTime;
		DateTime t2 = t1.AddMinutes(30);
		DateTime t3 = t2.AddMinutes(30);
		DateTime t4 = t3.AddMinutes(30);
		DateTime t5 = t4.AddMinutes(30);
		var allProgrammes = EpgParser.ParseEpgDoc(_outputStore.Epg ?? new XDocument(), 0.0).Programmes;
		var programmesHtml = new StringBuilder();
		EpgProgrammeList programmes;
		foreach (SelectedChannel channel in _configStore.SelectedChannels)
		{
			programmesHtml.Append($"<tr><td class=\"preview-channel-column\"><img src=\"{channel.LogoUri}\" />{channel.Name}</td>");
			programmes = new EpgProgrammeList(allProgrammes.Where(p => p.ChannelId == channel.Id && (p.StartTime.IsWithin(t1, t5) || p.EndTime.IsWithin(t1, t5))).OrderBy(p => p.StartTime));
			for (int slot = 0; slot < 5; slot++)
			{
				try
				{
					int slots = (int)Math.Ceiling((programmes[slot].EndTime - (programmes[slot].StartTime < t1 ? t1 : programmes[slot].StartTime)).TotalMinutes / 30.0);
					slots = slots > (5 - slot) ? (5 - slot) : slots < 1 ? 1 : slots;
					programmesHtml.Append($"<td class=\"preview-timeslot-column\" colspan=\"{slots}\">{programmes[slot].Title}<br />{programmes[slot].StartTime:t} - {programmes[slot].EndTime:t}</td>");
					slot += slots - 1;
				}
				catch
				{
					programmesHtml.Append($"<td class=\"preview-timeslot-column\" colspan=\"{5 - slot}\">No Info</td>");
					slot = 5;
				}
			}
			programmesHtml.AppendLine("</tr>");
		}
		string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "preview.partial.html");
		string previewHtml = File.ReadAllText(templatePath);
		previewHtml = string.Format(previewHtml, t1, t2, t3, t4, t5, programmesHtml.ToString());
		return previewHtml;
	}

	internal string GenerateReviewContent()
	{
		double totalChannels = _configStore.SelectedChannels.Count();
		ChannelMappingList allMappings = new ChannelMappingList(_configStore.SelectedChannels.SelectMany(c => c.EpgChannelIds));
		/*<div class="review-epg" id="review-epg-{0}"><div id="review-epg-{0}-name">{1}</div><div id="review-epg-{0}-coverage">{2}</div><div id="review-epg-{0}-accuracy">{3}</div></div>*/
		string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reviewepgsource.partial.html");
		string epgsourcesHtml = File.ReadAllText(templatePath);
		epgsourcesHtml = string.Join("\n", _configStore.SourceConfig.EpgUrls.OrderBy(s=>s.Priority).ThenBy(s => s.Name).Select(es => string.Format(epgsourcesHtml,
			es.Id,
			es.Name,
			allMappings[es.Id].Count() / totalChannels,
			_cacheStore.ReviewFeedback.GetAccuracyForSource(es.Id),
			string.Join(", ", _cacheStore.ReviewFeedback.Where(rf => rf.SourceId == es.Id).Select(rf => rf.Offset.ToString("0.0")).Distinct())
			)));

		DateTime startTime = DateTime.Today.AddHours(DateTime.Now.Hour);
		List<DateTime> slotTimes = [startTime];
		slotTimes.Add(slotTimes.Last().AddMinutes(30));
		slotTimes.Add(slotTimes.Last().AddMinutes(30));
		slotTimes.Add(slotTimes.Last().AddMinutes(30));
		slotTimes.Add(slotTimes.Last().AddMinutes(30));
		var allProgrammes = EpgParser.ParseEpgDoc(_outputStore.Epg ?? new XDocument(), 0.0).Programmes;
		var programmesHtml = new StringBuilder();
		EpgProgrammeList programmes;
		EpgProgramme programme;
		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reviewchannel.partial.html");
		string channelHtmlTemplate = File.ReadAllText(templatePath);
		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "reviewfeedback.partial.html");
		string feedbackHtmlTemplate = File.ReadAllText(templatePath);
		foreach (SelectedChannel channel in _configStore.SelectedChannels)
		{
			bool firstSource = true;
			if ((channel.EpgChannelIds?.Count ?? 0) < 1)
			{
				programmesHtml.Append($"<tr data-channelid=\"{channel.Id}\" data-sourceid=\"N/A\">");
				programmesHtml.Append($"<td class=\"review-channel-column\" rowspan=\"1\">{string.Format(channelHtmlTemplate, channel.LogoUri, channel.Name)}</td>");
				programmesHtml.Append($"<td class=\"review-feedback-column\" rowspan=\"1\">{string.Format(feedbackHtmlTemplate, string.Empty, string.Empty)}</td>");
				programmesHtml.Append($"<td class=\"review-timeslot-column\" colspan=\"5\">No Info</td>");
				programmesHtml.AppendLine("</tr>");
			}
			else
			{
				foreach (ChannelMapping mapping in (channel?.EpgChannelIds ?? []).OrderBy(m => _configStore.SourceConfig.EpgUrls.First(es => es.Id == m.SourceId).Priority))
				{
					programmesHtml.Append($"<tr data-channelid=\"{channel.Id}\" data-sourceid=\"{mapping.SourceId}\">");
					if (firstSource) programmesHtml.Append($"<td class=\"review-channel-column\" rowspan=\"{channel.EpgChannelIds.Count()}\">{string.Format(channelHtmlTemplate, channel.LogoUri, channel.Name)}</td>");
					firstSource = false;
					var feedback = _cacheStore.ReviewFeedback.FirstOrDefault(rf => rf.SourceId == mapping.SourceId && rf.ChannelId == channel.Id);
					programmesHtml.Append($"<td class=\"review-feedback-column\" rowspan=\"1\">{string.Format(feedbackHtmlTemplate, feedback?.IsAccurate ?? false ? "checked" : string.Empty, feedback?.Offset ?? 0.0)}</td>");
					programmes = new EpgProgrammeList(_cacheStore.EpgProgrammes[mapping.SourceId].Where(p => p.ChannelId == mapping.EpgId && (p.StartTime.IsWithin(slotTimes[0], slotTimes[4].AddSeconds(-1)) || p.EndTime.IsWithin(slotTimes[0].AddSeconds(1), slotTimes[4]))).OrderBy(p => p.StartTime));
					int skipped = 0;
					for (int slot = 0; slot < 5; slot++)
					{
						try
						{
							if ((slot - skipped) < programmes.Count)
							{
								programme = programmes[slot - skipped];
								if (slotTimes[slot].IsWithin(programme.StartTime, programme.EndTime))
								{
									int slots = (int)Math.Ceiling((programme.EndTime - (programme.StartTime < slotTimes[0] ? slotTimes[0] : programme.StartTime)).TotalMinutes / 30.0);
									slots = slots > (5 - slot) ? (5 - slot) : slots < 1 ? 1 : slots;
									programmesHtml.Append($"<td class=\"review-timeslot-column\" colspan=\"{slots}\">{programme.Title}<br />{programme.Description}<br />{programme.StartTime:t} - {programme.EndTime:t}</td>");
									slot += slots - 1;
								}
								else
								{
									skipped++;
								}
							}
							else
							{
								programmesHtml.Append($"<td class=\"review-timeslot-column\" colspan=\"{skipped}\">No Info</td>");
							}
						}
						catch
						{
							programmesHtml.Append($"<td class=\"review-timeslot-column\" colspan=\"{5 - slot}\">No Info</td>");
							slot = 5;
						}
					}
					programmesHtml.AppendLine("</tr>");
				}
			}
		}
		templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "review.partial.html");
		string previewHtml = File.ReadAllText(templatePath);
		previewHtml = string.Format(previewHtml, epgsourcesHtml, slotTimes[0], slotTimes[1], slotTimes[2], slotTimes[3], slotTimes[4], programmesHtml.ToString());
		return previewHtml;
	}
}
