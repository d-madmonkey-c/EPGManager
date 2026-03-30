using System.Text;
using System.Text.RegularExpressions;
using EPGManager.Data;

namespace EPGManager;

public static class PrimaryOutputBuilder
{
	public static string Build(
		string originalM3u,
		List<Channel> channels,
		ConfigStore config)
	{
		var lines = originalM3u.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var sb = new StringBuilder();

		sb.AppendLine("#EXTM3U");

		// Build a lookup of tvg-id -> (extinf, url)
		//var map = BuildChannelMap(lines);

		// Iterate in the user-defined order. Allow each selected channel to appear in multiple groups.
		var addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (SelectedChannel selectedChannel in config.SelectedChannels)
		{
			var tvgId = selectedChannel.Id;
			// if (!map.TryGetValue(tvgId, out var entry))
			// 	continue;

			var groupTitles = selectedChannel.Groups?.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? ["Ungrouped"];
			// if (!groupTitles.Any())
			// {
			// 	var fallback = selectedChannel.EffectiveGroupTitle?.Trim();
			// 	if (!string.IsNullOrWhiteSpace(fallback))
			// 		groupTitles.Add(fallback);
			// 	else
			// 		groupTitles.Add("Ungrouped");
			// }

			foreach (var groupTitle in groupTitles)
			{
				var key = $"{tvgId}||{groupTitle}";
				if (addedEntries.Contains(key))
					continue;

				addedEntries.Add(key);
/*
				var extInf = ApplySelectedChannelOverrides(entry.ExtInf, selectedChannel);
				extInf = Regex.Replace(extInf, "group-title=\"[^\"]*\"", $"group-title=\"{groupTitle}\"");

				sb.AppendLine(extInf);
				sb.AppendLine(entry.Url);*/
			}
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

	private static string ApplySelectedChannelOverrides(string extInf, SelectedChannelList config)
	{
		string updated = extInf;

/*/
		if (config.OverrideGroupTitle != null)
			updated = Regex.Replace(updated, "group-title=\"[^\"]*\"", $"group-title=\"{config.OverrideGroupTitle}\"");

		if (config.OverrideTvgName != null)
			updated = Regex.Replace(updated, "tvg-name=\"[^\"]*\"", $"tvg-name=\"{config.OverrideTvgName}\"");

		if (config.OverrideTvgLogo != null)
			updated = Regex.Replace(updated, "tvg-logo=\"[^\"]*\"", $"tvg-logo=\"{config.OverrideTvgLogo}\"");
*/
		return updated;
	}
}
