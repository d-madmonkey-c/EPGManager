namespace EPGManager.Data;

public class SelectedChannelList : List<SelectedChannel>;

public class SelectedChannel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // URL from the M3U file
    public string LogoUri { get; set; } = string.Empty; // URL from the M3U file
    public string Uri { get; set; } = string.Empty; // URL from the M3U file
    public List<string> Groups { get; set; } = [];
    public ChannelMappingList EpgChannelIds { get; set; } = []; /*ChannelId, SourceId, EpgId*/
}

/*\"EpgChannelIds\":[{\"SourceId\":\"user_639104094568940684_1\",\"SourceName\":\"EPG Best\",\"EpgId\":\"470288\"}]*/