using System;

namespace EPGManager.Data;

public class EpgSource
{
	public string Id { get; set; } = string.Empty; // Unique identifier for this source
	public string Name { get; set; } = string.Empty; // Display name for this source
	public string Url { get; set; } = string.Empty;
	public int Priority { get; set; } // Lower number = higher priority

}
