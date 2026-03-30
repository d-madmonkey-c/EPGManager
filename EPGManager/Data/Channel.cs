namespace EPGManager.Data;

public class ChannelList : List<Channel>;

public class Channel
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Group { get; set; }
	public string? LogoUri { get; set; }
	public string Uri { get; set; } = string.Empty; // URL from the M3U file
}