using System.Text.RegularExpressions;

namespace EPGManager.API;

public static class M3uParser
{
	private static readonly Regex ExtInfRegex =
		new(@"#EXTINF:-1\s+(?<attrs>.+?),(?<name>.+)$", RegexOptions.Compiled);

	private static readonly Regex AttrRegex =
		new(@"(?<key>[a-zA-Z0-9\-]+)=""(?<value>[^""]*)""", RegexOptions.Compiled);

	public static List<ChannelIdentity> Parse(string m3uContent)
	{
		var lines = m3uContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var result = new List<ChannelIdentity>();

		for (int i = 0; i < lines.Length - 1; i++)
		{
			var line = lines[i];
			if (!line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
				continue;

			var match = ExtInfRegex.Match(line);
			if (!match.Success) continue;

			var attrsPart = match.Groups["attrs"].Value;
			var displayName = match.Groups["name"].Value.Trim();

			var attrs = AttrRegex.Matches(attrsPart)
				.ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);

			attrs.TryGetValue("tvg-id", out var tvgId);
			attrs.TryGetValue("group-title", out var groupTitle);
			attrs.TryGetValue("tvg-name", out var tvgName);
			attrs.TryGetValue("tvg-logo", out var tvgLogo);

			if (string.IsNullOrWhiteSpace(tvgId))
				continue;

			result.Add(new ChannelIdentity
			{
				M3uTvgId = tvgId,
				Name = displayName,
				OriginalGroupTitle = groupTitle,
				OriginalTvgName = tvgName,
				OriginalTvgLogo = tvgLogo
			});

			i++; // skip URL
		}

		return result;
	}
}
