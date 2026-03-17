namespace EPGManager.API;

public class OutputStore
{
	public string? PrimaryOutput { get; set; }
	public string? SecondaryOutputXml { get; set; }
	public List<ChannelIdentity> AvailableChannels { get; set; } = new();
}