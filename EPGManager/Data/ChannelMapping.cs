using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EPGManager.Data;

public class ChannelMappingList : List<ChannelMapping>
{
	public ChannelMappingList() { }

	public ChannelMappingList(IEnumerable<ChannelMapping> items) : base(items) { }

	// TODO - this casues System.ArgumentException: An item with the same key has already been added. Key: 470718
	[JsonIgnore]
	public Dictionary<string, string> this[string sourceId]
	{
		get => this.Where(cm => cm.SourceId == sourceId).Select(cm => new { cm.EpgId, cm.ChannelId }).Distinct().ToDictionary(x => x.EpgId, x => x.ChannelId);
	}
}

public class ChannelMapping
{
	public string ChannelId { get; set; } = string.Empty;
	public string SourceId { get; set; } = string.Empty;
	public string EpgId { get; set; } = string.Empty;
}