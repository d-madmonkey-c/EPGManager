using System.Text;
using System.Text.RegularExpressions;

namespace EPGManager.API;

public static class PrimaryOutputBuilder
{
	public static string Build(
		string originalM3u,
		List<ChannelIdentity> channels,
		SourceConfig config)
	{
		var lines = originalM3u.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var sb = new StringBuilder();

		sb.AppendLine("#EXTM3U");

		// Build a lookup of tvg-id -> (extinf, url)
		var map = BuildChannelMap(lines);

		// Iterate in the user-defined order
		foreach (var tvgId in config.SelectedChannels)
		{
			if (!map.TryGetValue(tvgId, out var entry))
				continue;

			// Apply rules
			var rule = config.RecategorizationRules.FirstOrDefault(r => tvgId.Equals(r.MatchTvgId, StringComparison.OrdinalIgnoreCase));

			if (rule?.Hidden == true)
				continue;

			var extInf = ApplyRecategorization(entry.ExtInf, rule);

			sb.AppendLine(extInf);
			sb.AppendLine(entry.Url);
		}

		return sb.ToString();
	}

	private static Dictionary<string, (string ExtInf, string Url)> BuildChannelMap(string[] lines)
	{
		var dict = new Dictionary<string, (string, string)>();

		for (int i = 0; i < lines.Length - 1; i++)
		{
			var line = lines[i];
			if (!line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
				continue;

			var tvgId = ExtractTvgId(line);
			if (tvgId == null)
				continue;

			var url = lines[i + 1];
			dict[tvgId] = (line, url);

			i++;
		}

		return dict;
	}

	private static string? ExtractTvgId(string extInfLine)
	{
		var match = Regex.Match(extInfLine, "tvg-id=\"(?<id>[^\"]+)\"");
		return match.Success ? match.Groups["id"].Value : null;
	}

	private static string ApplyRecategorization(string extInf, RecategorizationRule? rule)
	{
		if (rule == null)
			return extInf;

		string updated = extInf;

		if (rule.NewGroupTitle != null)
			updated = Regex.Replace(updated, "group-title=\"[^\"]*\"", $"group-title=\"{rule.NewGroupTitle}\"");

		if (rule.NewTvgName != null)
			updated = Regex.Replace(updated, "tvg-name=\"[^\"]*\"", $"tvg-name=\"{rule.NewTvgName}\"");

		if (rule.NewTvgLogo != null)
			updated = Regex.Replace(updated, "tvg-logo=\"[^\"]*\"", $"tvg-logo=\"{rule.NewTvgLogo}\"");

		return updated;
	}
}
