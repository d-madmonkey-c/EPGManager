using System;

namespace EPGManager.Data;

public class EpgSource
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string Name { get; set; } = string.Empty; // Display name for this source
	public string Url { get; set; } = string.Empty;
	public int Priority { get; set; } // Lower number = higher priority
	public TimeSpan Offset { get; set; } = TimeSpan.Zero;
}
