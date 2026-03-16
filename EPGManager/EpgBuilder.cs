using System.Xml.Linq;

namespace EPGManager.API;

public static class SecondaryOutputBuilder
{
	public static XDocument Build(
		List<ChannelIdentity> channels,
		XDocument utcDoc,
		XDocument epgDoc)
	{
		var root = new XElement("tv");

		foreach (var ch in channels.Where(c => c.UtcId != null))
		{
			var utcChannel = utcDoc.Root!
				.Elements("channel")
				.FirstOrDefault(x => (string)x.Attribute("id") == ch.UtcId);

			if (utcChannel != null)
				root.Add(new XElement(utcChannel));
		}

		foreach (var ch in channels.Where(c => c.EpgCaId != null))
		{
			var epgChannel = epgDoc.Root!
				.Elements("channel")
				.FirstOrDefault(x => (string)x.Attribute("id") == ch.EpgCaId);

			if (epgChannel != null)
				root.Add(new XElement(epgChannel));
		}

		foreach (var ch in channels.Where(c => c.UtcId != null))
		{
			var progs = utcDoc.Root!
				.Elements("programme")
				.Where(p => (string)p.Attribute("channel") == ch.UtcId);

			foreach (var p in progs)
				root.Add(new XElement(p));
		}

		foreach (var ch in channels.Where(c => c.EpgCaId != null))
		{
			var progs = epgDoc.Root!
				.Elements("programme")
				.Where(p => (string)p.Attribute("channel") == ch.EpgCaId);

			foreach (var p in progs)
				root.Add(new XElement(p));
		}

		return new XDocument(root);
	}
}