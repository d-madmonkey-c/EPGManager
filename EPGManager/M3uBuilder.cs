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

		var mappedIds = new HashSet<string>(channels.Select(c => c.M3uTvgId));

		for (int i = 0; i < lines.Length - 1; i++)
		{
			var line = lines[i];
			if (!line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
				continue;

			var tvgId = ExtractTvgId(line);
			if (tvgId is null || !mappedIds.Contains(tvgId))
			{
				i++;
				continue;
			}

			var rule = config.RecategorizationRules
				.FirstOrDefault(r => r.MatchTvgId.Equals(tvgId, StringComparison.OrdinalIgnoreCase));

			if (rule?.Hidden == true)
			{
				i++;
				continue;
			}

			var newExtInf = ApplyRecategorization(line, rule);

			sb.AppendLine(newExtInf);
			sb.AppendLine(lines[i + 1]);
			i++;
		}

		return sb.ToString();
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
