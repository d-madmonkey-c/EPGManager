namespace EPGManager.Data;

public class SourceConfig
{
    public string M3uUrl { get; set; } = string.Empty;
    public List<EpgSource> EpgUrls { get; set; } = new();
}
