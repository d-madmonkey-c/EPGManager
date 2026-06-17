using System;

namespace EPGManager.Data;

public class ConfigUpdate
{
	public string PrimaryUrl { get; set; }
	public List<EpgSource> EpgUrls { get; set; }
	public SelectedChannelList SelectedChannels { get; set; }
	public ReviewFeedbackList ReviewFeedback { get; set; }
}