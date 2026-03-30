using EPGManager.Data;

namespace EPGManager;

public class OutputStore
{
	public string? PrimaryOutput { get; set; }
	public string? SecondaryOutputXml { get; set; }
	public List<Channel> AvailableChannels { get; set; } = new();
	public HashSet<string> NewChannelIds { get; set; } = new(); // Track which channels are newly discovered
}