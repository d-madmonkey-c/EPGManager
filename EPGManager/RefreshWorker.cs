using System.IO.Compression;
using System.Xml.Linq;

namespace EPGManager.API;

public class RefreshWorker : BackgroundService
{
	private readonly ILogger<RefreshWorker> _logger;
	private readonly ConfigStore _configStore;
	private readonly OutputStore _outputStore;
	private readonly HttpClient _httpClient = new();

	public RefreshWorker(
		ILogger<RefreshWorker> logger,
		ConfigStore configStore,
		OutputStore outputStore)
	{
		_logger = logger;
		_configStore = configStore;
		_outputStore = outputStore;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var timer = new PeriodicTimer(TimeSpan.FromHours(24));

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await RefreshAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during refresh");
			}

			await timer.WaitForNextTickAsync(stoppingToken);
		}
	}

	private async Task RefreshAsync(CancellationToken ct)
	{
		var config = await _configStore.LoadAsync();

		if (string.IsNullOrWhiteSpace(config.PrimaryUrl))
		{
			_logger.LogInformation("Primary URL not configured.");
			return;
		}

		var m3uText = await DownloadText(config.PrimaryUrl, ct);
		var utcDoc = await DownloadAndParseGzipXml(config.UtcUrl, ct);
		var epgDoc = await DownloadAndParseGzipXml(config.EpgCaUrl, ct);

		// Parse channels
		var channels = M3uParser.Parse(m3uText);

		// Attach IDs
		ChannelMapper.AttachUtcIds(channels, utcDoc);
		ChannelMapper.AttachEpgCaIds(channels, epgDoc);

		// Store available channels for UI
		_outputStore.AvailableChannels = channels;

		// Build outputs
		var primaryOut = PrimaryOutputBuilder.Build(m3uText, channels, config);
		var secondaryOut = SecondaryOutputBuilder.Build(channels, utcDoc, epgDoc);

		_outputStore.PrimaryOutput = primaryOut;
		_outputStore.SecondaryOutputXml = secondaryOut.ToString();

		_logger.LogInformation("Refresh completed.");
	}

	private async Task<string> DownloadText(string url, CancellationToken ct)
	{
		using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(ct);
	}

	private async Task<XDocument> DownloadAndParseGzipXml(string url, CancellationToken ct)
	{
		using var response = await _httpClient.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		await using var stream = await response.Content.ReadAsStreamAsync(ct);
		await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
		return XDocument.Load(gzip);
	}
}