using System.Runtime;

namespace EPGManager.Data;

public class Channel
{
	public string Id { get; set; } = string.Empty;
	public string TvgId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Group { get; set; }
	public List<string> Groups { get; set; } = [];
	public string? LogoUri { get; set; }
	public string Uri { get; set; } = string.Empty; // URL from the M3U file
	public List<ChannelSource> Sources { get; set; } = [];

	public Channel()
	{
		Id = Guid.NewGuid().ToString();
	}

	public Channel(ChannelSource source) : this()
	{
		this.AddSource(source);
	}

	public void AddSource(ChannelSource source)
	{
		// if (!string.Equals(TvgId, source.TvgId, StringComparison.Ordinal))
		// 	throw new ArgumentException("Source tvg-id does not match this channel.", nameof(source));
		if (string.IsNullOrEmpty(source.TvgId))
			throw new ArgumentException("A source must have a tvg-id.", nameof(source));

		int existingIndex = Sources.FindIndex(existing => existing.SourceId == source.SourceId);

		if (existingIndex >= 0)
			Sources[existingIndex] = source;
		else
			Sources.Add(source);

		RecalculateEffectiveValues();
	}

	public void RemoveSource(ChannelSource source)
	{
		int existingIndex = Sources.FindIndex(existing => existing.SourceId == source.SourceId);
		if (existingIndex >= 0)
		{
			Sources.RemoveAt(existingIndex);
			RecalculateEffectiveValues();
		}
	}

	private void RecalculateEffectiveValues()
	{
		var ordered = Sources.OrderBy(source => source.Priority).ToList();

		TvgId = ordered.Select(source => source.TvgId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
		Name = ordered.Select(source => source.Name).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
		Group = ordered.Select(source => source.Group).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
		Groups = ordered
			.SelectMany(source => source.Groups.Append(source.Group))
			.Where(group => !string.IsNullOrWhiteSpace(group))
			.Select(group => group!)
			.Distinct(StringComparer.Ordinal)
			.ToList();
		LogoUri = ordered.Select(source => source.LogoUri).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
		Uri = ordered.Select(source => source.Uri).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
	}
}

public class ChannelList : List<Channel>
{
	public void Add(ChannelSource source)
	{
		if (string.IsNullOrWhiteSpace(source.TvgId))
			throw new ArgumentException("A source must have a tvg-id.", nameof(source));

		Channel? target = null;
		Channel[] matches = ChannelMatcher.FindMatches(source, this);
		if (matches == null || matches.Length == 0)
		{
			target = new Channel();
			base.Add(target);
		}
		else
		{
			target = matches[0];
			if (matches.Length > 1)
			{
				foreach (ChannelSource duplicate in matches.Skip(1).SelectMany(c => c.Sources))
				{
					target.AddSource(duplicate);
				}
				foreach (Channel duplicate in matches.Skip(1))
				{
					base.Remove(duplicate);
				}
			}
		}
		target.AddSource(source);
	}

	new public void Add(Channel channel)
	{
		foreach (ChannelSource source in channel.Sources)
		{
			Add(source);
		}
	}

	public void Remove(ChannelSource source)
	{
		Channel[] target = ChannelMatcher.FindMatches(source, this);
		if (target != null && target.Length == 1)
		{
			target[0].RemoveSource(source);
			if (target[0].Sources.Count <= 0)
			{
				base.Remove(target[0]);
			}
		}
		else
		{
			//Log
		}
	}

	new public void Remove(Channel channel)
	{
		foreach (ChannelSource source in channel.Sources)
		{
			Remove(source);
		}
	}

	public void Combine(ChannelList other)
	{
		foreach (Channel channel in other)
		{
			Add(channel);
		}
	}

	internal static class ChannelMatcher
	{
		public static Channel[] FindMatches(ChannelSource source, IEnumerable<Channel> channels)
		{
			if (string.IsNullOrWhiteSpace(source.TvgId)) return [];

			Channel[] matches = channels
				.Where(channel => string.Equals(channel.TvgId, source.TvgId, StringComparison.Ordinal))
				.ToArray();

			// if (matches.Length > 1)
			// 	throw new InvalidOperationException($"Multiple channels already exist for tvg-id '{source.TvgId}'.");

			return matches;
		}
	}
}
