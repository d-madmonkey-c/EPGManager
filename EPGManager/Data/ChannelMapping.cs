using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EPGManager.Data;

public class ChannelMappingList : List<ChannelMapping>
{
	public ChannelMappingList() { }

	public ChannelMappingList(IEnumerable<ChannelMapping> items) : base(items) { }

	[JsonIgnore]
	public Dictionary<string, string> this[string sourceId]
	{
		get => this.Where(cm => cm.SourceId == sourceId).Select(cm => new { cm.EpgId, cm.ChannelId }).ToDictionary(x => x.EpgId, x => x.ChannelId);
	}

	[JsonIgnore]
	public string this[string sourceId, string epgId]
	{
		get => this.FirstOrDefault(cm => cm.SourceId == sourceId && cm.EpgId == epgId)?.ChannelId ?? epgId;
	}
}

public class ChannelMapping
{
	public string ChannelId { get; set; } = string.Empty;
	public string SourceId { get; set; } = string.Empty;
	public string EpgId { get; set; } = string.Empty;
}