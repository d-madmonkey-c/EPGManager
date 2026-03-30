using System;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class EpgParser
{
	private const string PROGRAMME_TIME_FORMAT = "yyyyMMddHHmmss zzz";

	public static (EpgChannelList Channels, EpgProgrammeList Programmes) ParseEpgDoc(XDocument doc)
	{
		EpgChannelList channels = new EpgChannelList(doc.Root?
			.Elements("channel")
			.Select(ch => new EpgChannel
			{
				Id = (string)ch.Attribute("id")!,
				Name = (string)ch.Element("display-name")!,
				LogoUri = (string?)ch.Element("icon")?.Attribute("src") ?? string.Empty
			})
			.ToList() ?? []);

		EpgProgrammeList programmes = new EpgProgrammeList(doc.Root?
			.Elements("programme")
			.Select(pr => new EpgProgramme
			{
				ChannelId = (string)pr.Attribute("channel")!,
				Title = (string)pr.Element("title")!,
				Description = (string?)pr.Element("desc") ?? string.Empty,
				StartTime = DateTime.ParseExact((string)pr.Attribute("start")!, PROGRAMME_TIME_FORMAT, null),
				EndTime = DateTime.ParseExact((string)pr.Attribute("stop")!, PROGRAMME_TIME_FORMAT, null)
			})
			.ToList() ?? []);
		/*
		foreach (var selectedChannel in _configStore.SelectedChannels)
		{
			if (source.Id != null && selectedChannel.EpgChannelIds.ContainsKey(source.Id))
				continue; // Already has a mapping for this source by id

			// Backward compatibility: if old key is stored by source name, migrate it
			if (!string.IsNullOrEmpty(source.Name) && selectedChannel.EpgChannelIds.ContainsKey(source.Name))
			{
				selectedChannel.EpgChannelIds[source.Id ?? source.Name] = selectedChannel.EpgChannelIds[source.Name];
				selectedChannel.EpgChannelIds.Remove(source.Name);
				continue;
			}

			// Try to find a match by existing EPG ID from other sources
			string? foundId = null;
			foreach (var otherSourceId in selectedChannel.EpgChannelIds.Keys)
			{
				if (selectedChannel.EpgChannelIds.TryGetValue(otherSourceId, out var otherEpgId))
				{
					// Look for the same EPG ID in this source
					if (epgChannels.Any(c => c.Id == otherEpgId))
					{
						foundId = otherEpgId;
						break;
					}
				}
			}

			// If not found by ID, try by name matching
			if (foundId == null)
			{
				var match = epgChannels.FirstOrDefault(c =>
					string.Equals(c.Id, selectedChannel.Id, StringComparison.OrdinalIgnoreCase) ||
					Normalize(c.Name) == Normalize(selectedChannel.Name));

				if (match != null)
				{
					foundId = match.Id;
				}
			}

			if (foundId != null)
			{
				selectedChannel.EpgChannelIds[source.Id ?? source.Name] = foundId;
			}
		}*/
		return (Channels: channels, Programmes: programmes);
	}
}
