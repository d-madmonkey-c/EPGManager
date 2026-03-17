namespace EPGManager.API;

public class SourceConfig
{
    public string PrimaryUrl { get; set; } = string.Empty;
    public string UtcUrl { get; set; } = string.Empty;
    public string EpgCaUrl { get; set; } = string.Empty;
    public List<string> SelectedChannels { get; set; } = new(); // ordered tvg-ids
    public List<string> GroupOrder { get; set; } = new();       // ordered group names
    public List<RecategorizationRule> RecategorizationRules { get; set; } = new();
}
