using System.Numerics;

namespace EPGManager.Data;

public class EpgChannelList : List<EpgChannel>
{
	public EpgChannelList() { }

	public EpgChannelList(IEnumerable<EpgChannel> items) : base(items) { }
}

public class EpgChannel
{
	public string Id { get; set; } = string.Empty; // Unique identifier for this channel in the EPG
	public string Name { get; set; } = string.Empty;
	public string LogoUri { get; set; } = string.Empty;
}
