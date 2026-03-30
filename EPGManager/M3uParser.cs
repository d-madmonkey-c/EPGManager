using System.Text.RegularExpressions;
using EPGManager.Data;

namespace EPGManager;

public static class M3uParser
{
	private static readonly Regex ExtInfRegex =
		new(@"#EXTINF:-1\s+(?<attrs>.+?),(?<name>.+)$", RegexOptions.Compiled);

	private static readonly Regex AttrRegex =
		new(@"(?<key>[a-zA-Z0-9\-]+)=""(?<value>[^""]*)""", RegexOptions.Compiled);

	public static ChannelList Parse(string m3uContent)
	{
		var lines = m3uContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var result = new ChannelList();

		for (int i = 0; i < lines.Length - 1; i++)
		{
			var line = lines[i];
			if (!line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
				continue;

			var match = ExtInfRegex.Match(line);
			if (!match.Success) continue;

			var attrsPart = match.Groups["attrs"].Value;
			var displayName = match.Groups["name"].Value.Trim();

			var attrs = AttrRegex.Matches(attrsPart).ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);

			attrs.TryGetValue("tvg-id", out var tvgId);
			attrs.TryGetValue("group-title", out var groupTitle);
			attrs.TryGetValue("tvg-name", out var tvgName);
			attrs.TryGetValue("tvg-logo", out var tvgLogo);

			if (string.IsNullOrWhiteSpace(tvgId))
				continue;

			result.Add(new Channel
			{
				Id = tvgId,
				//Name = displayName,
				Name = displayName ?? tvgName,
				Group = groupTitle,
				//OriginalTvgName = tvgName,
				LogoUri = tvgLogo,
				Uri = lines[++i]
			});

			//i++; // skip URL
		}

		return result;
	}
}
