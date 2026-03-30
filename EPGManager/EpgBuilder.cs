using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public static class SecondaryOutputBuilder
{
	public static XDocument Build(
		List<Channel> channels,
		List<XDocument> secondaryDocs,
		List<EpgSource> sources,
		List<SelectedChannel> selectedChannels)
	{
		var root = new XElement("tv");
		var selectedChannelIds = new HashSet<string>(selectedChannels.Select(c => c.Id));

		foreach (var ch in channels.Where(c => selectedChannelIds.Contains(c.Id)))
		{
			var selectedConfig = selectedChannels.First(c => c.Id == ch.Id);

			// Add channel metadata from highest priority source
			XElement? channelElement = null;
			int highestPriority = int.MaxValue;
			for (int i = 0; i < sources.Count; i++)
			{
				var source = sources[i];
				// Use EPG IDs from selected config, falling back to auto-detected ones
				var sourceChannelId = /*selectedConfig.EpgChannelIds.GetValueOrDefault(source.Name) ??*/ ch.Id;

				if (!string.IsNullOrEmpty(sourceChannelId))
				{
					var sourceChannel = secondaryDocs[i].Root!
						.Elements("channel")
						.FirstOrDefault(x => (string)x.Attribute("id") == sourceChannelId);
					if (sourceChannel != null && source.Priority < highestPriority)
					{
						channelElement = new XElement(sourceChannel);
						channelElement.SetAttributeValue("id", ch.Id);
						highestPriority = source.Priority;
					}
				}
			}
			if (channelElement != null)
				root.Add(channelElement);

			// Collect programmes with priorities
			var programmes = new List<(XElement prog, int priority)>();
			for (int i = 0; i < sources.Count; i++)
			{
				var source = sources[i];
				// Use EPG IDs from selected config, falling back to auto-detected ones
				var sourceChannelId = /*selectedConfig.EpgChannelIds.GetValueOrDefault(source.Name) ??*/ ch.Id;

				if (!string.IsNullOrEmpty(sourceChannelId))
				{
					var progs = secondaryDocs[i].Root!
						.Elements("programme")
						.Where(p => (string)p.Attribute("channel") == sourceChannelId)
						.Select(p => (new XElement(p), source.Priority));
					programmes.AddRange(progs);
				}
			}

			// Sort by priority ascending
			programmes.Sort((a, b) => a.priority.CompareTo(b.priority));

			// Add non-overlapping programmes
			var addedProgrammes = new List<(DateTime start, DateTime stop)>();
			foreach (var (prog, _) in programmes)
			{
				var startStr = (string)prog.Attribute("start")!;
				var stopStr = (string)prog.Attribute("stop")!;
				if (DateTime.TryParse(startStr, out var start) && DateTime.TryParse(stopStr, out var stop))
				{
					bool overlaps = addedProgrammes.Any(ap => ap.start < stop && ap.stop > start);
					if (!overlaps)
					{
						prog.SetAttributeValue("channel", ch.Id);
						root.Add(prog);
						addedProgrammes.Add((start, stop));
					}
				}
			}
		}

		return new XDocument(root);
	}
}