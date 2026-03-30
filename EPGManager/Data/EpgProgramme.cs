using System;

namespace EPGManager.Data;

public class EpgProgrammeList : List<EpgProgramme>
{
	public EpgProgrammeList() { }

	public EpgProgrammeList(IEnumerable<EpgProgramme> items) : base(items) { }
}

public class EpgProgramme
{
	public string ChannelId { get; set; } = string.Empty; // EPG channel ID
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
}
