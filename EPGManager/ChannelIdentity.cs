namespace EPGManager.API;

public class ChannelIdentity
{
	public string M3uTvgId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;

	public string? OriginalGroupTitle { get; set; }
	public string? OriginalTvgName { get; set; }
	public string? OriginalTvgLogo { get; set; }

	public string? UtcId { get; set; }
	public string? EpgCaId { get; set; }

}