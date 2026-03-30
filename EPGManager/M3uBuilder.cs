using System.Text;
using System.Text.RegularExpressions;
using EPGManager.Data;

namespace EPGManager;

public static class M3uBuilder
{
	//private const string CHANNEL_DESCRIPTOR = $"#EXTINF:-1 tvg-id=\"{0}\" tvg-name=\"{1}\" tvg-logo=\"{2}\" group-title=\"{3}\",{1}";

	public static string Build(SelectedChannelList channels)
	{
		var sb = new StringBuilder();

		sb.AppendLine("#EXTM3U url-tvg=\"http://m3u4u.com/epg/jwmzn12xpqhkp8xky721\"");

		var groups = channels.Select(c => c.Groups[0]).Distinct();
		foreach (string group in groups)
		{
			var groupChannels = channels.Where(c => c.Groups.Contains(group));
			foreach (SelectedChannel channel in groupChannels)
			{
				sb.AppendLine($"#EXTINF:-1 tvg-id=\"{channel.Id}\" tvg-name=\"{channel.Name}\" tvg-logo=\"{channel.LogoUri}\" group-title=\"{group}\",{channel.Name}");
				sb.AppendLine(channel.Uri);
			}
		}
		return sb.ToString();
	}

/*
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

/ *
		if (config.OverrideGroupTitle != null)
			updated = Regex.Replace(updated, "group-title=\"[^\"]*\"", $"group-title=\"{config.OverrideGroupTitle}\"");

		if (config.OverrideTvgName != null)
			updated = Regex.Replace(updated, "tvg-name=\"[^\"]*\"", $"tvg-name=\"{config.OverrideTvgName}\"");

		if (config.OverrideTvgLogo != null)
			updated = Regex.Replace(updated, "tvg-logo=\"[^\"]*\"", $"tvg-logo=\"{config.OverrideTvgLogo}\"");
* /
		return updated;
	}
	*/
}
