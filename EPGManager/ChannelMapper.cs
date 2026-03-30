using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public static class ChannelMapper
{
	public static void AttachSecondaryIds(List<Channel> channels, List<XDocument> secondaryDocs, List<EpgSource> sources)
	{
		for (int i = 0; i < sources.Count; i++)
		{
			var doc = secondaryDocs[i];
			var source = sources[i];
			var sourceChannels = doc.Root!
				.Elements("channel")
				.Select(ch => new
				{
					Id = (string)ch.Attribute("id")!,
					Name = (string)ch.Element("display-name")!
				})
				.ToList();

			foreach (var ch in channels)
			{
				var match = sourceChannels.FirstOrDefault(s =>
					string.Equals(s.Id, ch.Id, StringComparison.OrdinalIgnoreCase) ||
					Normalize(s.Name) == Normalize(ch.Name));

				//if (match != null)
					//ch.[source.Name] = match.Id;
			}
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