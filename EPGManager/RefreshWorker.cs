using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class RefreshWorker : BackgroundService
{
	private readonly static TimeSpan _refreshTime = new TimeSpan(4, 0, 0);

	private readonly ILogger<RefreshWorker> _logger;
	private readonly Processor _processor;

	public RefreshWorker(
		ILogger<RefreshWorker> logger,
		Processor processor)
	{
		_logger = logger;
		_processor = processor;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("RefreshWorker started.");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				// Calculate next run time
				var now = DateTime.Now;
				var nextRun = now.Date + _refreshTime;

				if (now > nextRun)
					nextRun = nextRun.AddDays(1);

				var delay = nextRun - now;
				_logger.LogInformation("Next run scheduled at: {NextRun}", nextRun);

				// Wait until the scheduled time
				await Task.Delay(delay, stoppingToken);

				// Run the task
				await RefreshAsync(stoppingToken);

				// Wait before looping to help.
				await Task.Delay(new TimeSpan(1,0,0), stoppingToken);
			}
			catch (TaskCanceledException)
			{
				// Service is stopping
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred in DailyTaskService.");
			}
		}

		_logger.LogInformation("DailyTaskService stopped.");
	}

	private async Task RefreshAsync(CancellationToken ct)
	{
		_logger.LogInformation("RefreshWorker: Refreshing M3U.");
		await _processor.RefreshM3uAsync();
		_logger.LogInformation("RefreshWorker: Refreshing EPGs.");
		await _processor.RefreshEpgAsync();
		_logger.LogInformation("RefreshWorker: Generating outputs.");
		await _processor.GenerateOutputs();
		_processor.LastRefresh = DateTime.Now;
		_logger.LogInformation("Refresh completed.");
	}
}