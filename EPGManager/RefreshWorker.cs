using System.IO.Compression;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class RefreshWorker : BackgroundService
{
	private readonly ILogger<RefreshWorker> _logger;
	private readonly ConfigStore _configStore;
	private readonly OutputStore _outputStore;
	private readonly CacheStore _cacheStore;
	private readonly HttpClient _httpClient = new();

	public RefreshWorker(
		ILogger<RefreshWorker> logger,
		ConfigStore configStore,
		OutputStore outputStore,
		CacheStore cacheStore)
	{
		_logger = logger;
		_configStore = configStore;
		_outputStore = outputStore;
		_cacheStore = cacheStore;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// var timer = new PeriodicTimer(TimeSpan.FromHours(24));

		// while (!stoppingToken.IsCancellationRequested)
		// {
		// 	await timer.WaitForNextTickAsync(stoppingToken);

		// 	try
		// 	{
		// 		await RefreshAsync(stoppingToken);
		// 	}
		// 	catch (Exception ex)
		// 	{
		// 		_logger.LogError(ex, "Error during refresh");
		// 	}

		// }
	}

	private async Task RefreshAsync(CancellationToken ct)
	{
		//_configStore.LoadAll();
		var config = _configStore.SourceConfig;

		if (string.IsNullOrWhiteSpace(config.M3uUrl))
		{
			_logger.LogInformation("Primary URL not configured.");
			return;
		}

		//var m3uText = await DownloadText(config.M3uUrl, ct);
		// var secondarySources = new List<EpgSource>();
		// var secondaryDocs = new List<XDocument>();
		// foreach (var source in secondarySources)
		// {			var doc = await DownloadAndParseGzipXml(source.Url, ct);
		// 	secondaryDocs.Add(doc);
		// }

		// Parse channels
		//var channels = M3uParser.Parse(m3uText);

		// Attach IDs
		//ChannelMapper.AttachSecondaryIds(channels, secondaryDocs, secondarySources);

		// Store available channels for UI
		//_outputStore.AvailableChannels = channels;

		// Build outputs
		//var primaryOut = PrimaryOutputBuilder.Build(m3uText, channels, config);
		//var secondaryOut = SecondaryOutputBuilder.Build(channels, secondaryDocs, secondarySources, config.SelectedChannels);

		//_outputStore.PrimaryOutput = primaryOut;
		//_outputStore.SecondaryOutputXml = secondaryOut.ToString();

		_logger.LogInformation("Refresh completed.");
	}

	/// <summary>
	/// Loads cached M3U on startup
	/// </summary>
	public async Task LoadCachedChannelsAsync()
	{
		try
		{
			var cachedM3u = await _cacheStore.LoadM3uCacheAsync();
			if (!string.IsNullOrWhiteSpace(cachedM3u))
			{
				var channels = M3uParser.Parse(cachedM3u);
				//_outputStore.AvailableChannels = channels;
				_logger.LogInformation($"Loaded {channels.Count} channels from cache on startup.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading cached channels on startup");
		}
	}

	private static string Normalize(string? s)
	{
		if (string.IsNullOrWhiteSpace(s)) return string.Empty;
		return s.ToLowerInvariant()
				.Replace("hd", "")
				.Replace("(", "")
				.Replace(")", "")
				.Replace("  ", " ")
				.Trim();
	}

	public async Task TriggerRefreshAsync(CancellationToken ct = default)
	{
		await RefreshAsync(ct);
	}
}