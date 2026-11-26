namespace EPGManager.Data;

public class SourceConfig
{
    public List<M3uSource> M3uUrls { get; set; } = [];
    public List<EpgSource> EpgUrls { get; set; } = [];
}
