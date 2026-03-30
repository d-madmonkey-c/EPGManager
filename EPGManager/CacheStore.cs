using System.IO.Compression;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class CacheStore
{
	private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "cache");

	public ChannelList Channels { get; set; } = new ChannelList();
	public List<string> NewChannelIds { get; set; } = new List<string>();
	public Dictionary<string, EpgChannelList> EpgChannels { get; set; } = new Dictionary<string, EpgChannelList>();
	public Dictionary<string, EpgProgrammeList> EpgProgrammes { get; set; } = new Dictionary<string, EpgProgrammeList>();

	public CacheStore()
	{
		Directory.CreateDirectory(_cacheDir);
	}

	public void LoadAll(List<string> epgIds)
	{
		Channels = Utility.LoadJson<ChannelList>(Path.Combine(_cacheDir, "channels.json")) ?? new ChannelList();
		NewChannelIds = Utility.LoadJson<List<string>>(Path.Combine(_cacheDir, "new_channels.json")) ?? new List<string>();
		foreach (var epgId in epgIds)
		{
			var epgChannels = Utility.LoadJson<EpgChannelList>(Path.Combine(_cacheDir, $"epg_{epgId}_channels.json")) ?? new EpgChannelList();
			EpgChannels[epgId] = epgChannels;

			var epgProgrammes = Utility.LoadJson<EpgProgrammeList>(Path.Combine(_cacheDir, $"epg_{epgId}_programmes.json")) ?? new EpgProgrammeList();
			EpgProgrammes[epgId] = epgProgrammes;
		}
	}

	public void SaveAll()
	{
		Utility.SaveJson(Path.Combine(_cacheDir, "channels.json"), Channels);
		Utility.SaveJson(Path.Combine(_cacheDir, "new_channels.json"), NewChannelIds);
		foreach (var kvp in EpgChannels)
		{
			var epgId = kvp.Key;
			var epgChannels = kvp.Value;
			Utility.SaveJson(Path.Combine(_cacheDir, $"epg_{epgId}_channels.json"), epgChannels);
		}

		foreach (var kvp in EpgProgrammes)
		{
			var epgId = kvp.Key;
			var epgProgrammes = kvp.Value;
			Utility.SaveJson(Path.Combine(_cacheDir, $"epg_{epgId}_programmes.json"), epgProgrammes);
		}
	}

	/// <summary>
	/// Saves M3U content to cache
	/// </summary>
	public async Task SaveM3uCacheAsync(string content)
	{
		var path = Path.Combine(_cacheDir, "channels.m3u");
		await File.WriteAllTextAsync(path, content);
	}

	/// <summary>
	/// Loads M3U content from cache if it exists
	/// </summary>
	public async Task<string?> LoadM3uCacheAsync()
	{
		var path = Path.Combine(_cacheDir, "channels.m3u");
		if (!File.Exists(path))
			return null;

		return await File.ReadAllTextAsync(path);
	}

	/// <summary>
	/// Saves EPG XML content to cache for a specific source
	/// </summary>
	public async Task SaveEpgCacheAsync(string sourceId, XDocument doc)
	{
		var path = Path.Combine(_cacheDir, $"epg_source_{sourceId}.xml.gz");
		using (var stream = File.Create(path))
		using (var gzip = new GZipStream(stream, CompressionMode.Compress))
		{
			await doc.SaveAsync(gzip, SaveOptions.None, CancellationToken.None);
		}
	}

	/// <summary>
	/// Loads EPG XML from cache for a specific source
	/// </summary>
	public async Task<XDocument?> LoadEpgCacheAsync(string sourceId)
	{
		var path = Path.Combine(_cacheDir, $"epg_source_{sourceId}.xml.gz");
		if (!File.Exists(path))
			return null;

		using (var stream = File.OpenRead(path))
		using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
		{
			return XDocument.Load(gzip);
		}
	}

	/// <summary>
	/// Clears all cache files
	/// </summary>
	public void ClearCache()
	{
		if (Directory.Exists(_cacheDir))
		{
			foreach (var file in Directory.GetFiles(_cacheDir))
			{
				File.Delete(file);
			}
		}
	}
}
