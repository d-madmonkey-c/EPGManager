namespace EPGManager.API;

public class RecategorizationRule
{
	public string MatchTvgId { get; set; } = string.Empty;
	public string? NewGroupTitle { get; set; }
	public string? NewTvgName { get; set; }
	public string? NewTvgLogo { get; set; }
	public bool Hidden { get; set; }
}
