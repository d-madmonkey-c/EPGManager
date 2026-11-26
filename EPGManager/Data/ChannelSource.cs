using System;

namespace EPGManager.Data;

public class ChannelSource
{
	public string SourceId { get; set; } = string.Empty;
	public int Priority { get; set; }
	public string TvgId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Group { get; set; }
	public string? LogoUri { get; set; }
	public string Uri { get; set; } = string.Empty;
}
