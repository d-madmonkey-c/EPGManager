namespace EPGManager.API;

public class SourceConfig
{
    public string PrimaryUrl { get; set; } = string.Empty;
    public string UtcUrl { get; set; } = string.Empty;
    public string EpgCaUrl { get; set; } = string.Empty;

    public List<RecategorizationRule> RecategorizationRules { get; set; } = new();
}
