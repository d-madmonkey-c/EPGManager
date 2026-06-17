using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public static class EpgBuilder
{
	private const string PROGRAMME_TIME_FORMAT = "yyyyMMddHHmmss zzz";

	public static XDocument Build(SelectedChannelList channels, List<EpgSource> sources, CacheStore cacheStore)
	{
		ChannelMappingList channelMappings = new ChannelMappingList(channels.SelectMany(c => c.EpgChannelIds));
		EpgProgrammeList selectedProgrammes = new EpgProgrammeList();

		var root = new XElement("tv");
		//root.SetAttributeValue("date", DateTime.Now.ToString(PROGRAMME_TIME_FORMAT));
		root.SetAttributeValue("generator-info-name", "EPG Manager");
		root.SetAttributeValue("generator-info-url", "http://192.168.10.101:5000"); // TODO : Host Url, same goes for logos.

		// List the channels
		XElement channelElement, displayNameElement, iconElement;
		foreach (SelectedChannel channel in channels)
		{
			channelElement = new XElement("channel");
			channelElement.SetAttributeValue("id", channel.Id);
			displayNameElement = new XElement("display-name");
			displayNameElement.SetAttributeValue("lang", "en");
			displayNameElement.Value = channel.Name;
			channelElement.Add(displayNameElement);
			iconElement = new XElement("icon");
			iconElement.SetAttributeValue("src", channel.LogoUri);
			channelElement.Add(iconElement);
			root.Add(channelElement);
		}

		XElement programmeElement, titleElement, descElement;
		foreach (EpgSource source in sources.OrderBy(s => s.Priority).ThenBy(s => s.Name))
		{
			try
			{
				var mappings = channelMappings[source.Id];
				var programmes = cacheStore.EpgProgrammes[source.Id].Where(p => mappings.ContainsKey(p.ChannelId));
				string channelId;
				foreach (EpgProgramme programme in programmes)
				{
					channelId = mappings[programme.ChannelId];
					if (selectedProgrammes.Any(p =>
						p.ChannelId == channelId
						&& p.IsOverlapping(programme)
					)) continue;
					programmeElement = new XElement("programme");
					//programmeElement.SetAttributeValue("start", programme.StartTime.ToString(PROGRAMME_TIME_FORMAT));
					//programmeElement.SetAttributeValue("stop", programme.EndTime.ToString(PROGRAMME_TIME_FORMAT));
					programmeElement.SetAttributeValue("start", programme.StartTime.ToUniversalTime().ToString("yyyyMMddHHmmss +0000"));
					programmeElement.SetAttributeValue("stop", programme.EndTime.ToUniversalTime().ToString("yyyyMMddHHmmss +0000"));
					programmeElement.SetAttributeValue("channel", channelId);
					titleElement = new XElement("title");
					titleElement.SetAttributeValue("lang", "en");
					titleElement.SetValue(programme.Title);
					programmeElement.Add(titleElement);
					descElement = new XElement("desc");
					descElement.SetAttributeValue("lang", "en");
					descElement.SetValue(programme.Description);
					programmeElement.Add(descElement);
					root.Add(programmeElement);
					selectedProgrammes.Add(new EpgProgramme
					{
						ChannelId = channelId,
						Title = programme.Title,
						Description = programme.Description,
						StartTime = programme.StartTime,
						EndTime = programme.EndTime
					});
				}
			}catch(Exception ex)
			{

			}
		}

		return new XDocument(root);
	}
}