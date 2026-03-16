using System.Xml.Linq;

namespace EPGManager.API;

public static class ChannelMapper
{
	public static void AttachUtcIds(List<ChannelIdentity> channels, XDocument utcDoc)
	{
		var utcChannels = utcDoc.Root!
			.Elements("channel")
			.Select(ch => new
			{
				Id = (string)ch.Attribute("id")!,
				Name = (string)ch.Element("display-name")!
			})
			.ToList();

		foreach (var ch in channels)
		{
			var match = utcChannels.FirstOrDefault(u =>
				string.Equals(u.Id, ch.M3uTvgId, StringComparison.OrdinalIgnoreCase) ||
				Normalize(u.Name) == Normalize(ch.Name));

			if (match != null)
				ch.UtcId = match.Id;
		}
	}

	public static void AttachEpgCaIds(List<ChannelIdentity> channels, XDocument epgDoc)
	{
		var epgChannels = epgDoc.Root!
			.Elements("channel")
			.Select(ch => new
			{
				Id = (string)ch.Attribute("id")!,
				Name = (string)ch.Element("display-name")!
			})
			.ToList();

		foreach (var ch in channels)
		{
			var match = epgChannels.FirstOrDefault(e =>
				Normalize(e.Name) == Normalize(ch.Name));

			if (match != null)
				ch.EpgCaId = match.Id;
		}
	}

	private static string Normalize(string? s)
	{
		if (string.IsNullOrWhiteSpace(s)) return string.Empty;
		return s.ToLowerInvariant()
				.Replace("hd", "")
				.Replace("(", "")
				.Replace(")", "")
				.Replace("  ", " ")
				.Trim();
	}
}