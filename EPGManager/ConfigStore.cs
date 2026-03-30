using System.Text.Json;
using EPGManager.Data;

namespace EPGManager;

public class ConfigStore
{
	private readonly string SourceConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "sourceconfig.json");
	private readonly string SelectedChannelsPath = Path.Combine(AppContext.BaseDirectory, "config", "selectedchannels.json");

	public SourceConfig SourceConfig { get; private set; } = new SourceConfig();
	public SelectedChannelList SelectedChannels { get; private set; } = new SelectedChannelList();

	public void LoadAll()
	{
		SourceConfig = Utility.LoadJson<SourceConfig>(SourceConfigPath) ?? new SourceConfig();
		SelectedChannels = Utility.LoadJson<SelectedChannelList>(SelectedChannelsPath) ?? new SelectedChannelList();
	}

	public void SaveAll()
	{
		Utility.SaveJson(SourceConfigPath, SourceConfig);
		Utility.SaveJson(SelectedChannelsPath, SelectedChannels);
	}
}