using EPGManager.Data;

namespace EPGManager;

public class OutputStore
{
	private readonly string _m3uPath = Path.Combine(AppContext.BaseDirectory, "cache", "myguide.m3u");
	private readonly string _epgPath = Path.Combine(AppContext.BaseDirectory, "cache", "myguide.xml");

	public string? M3u
	{
		get
		{
			try
			{
				return File.ReadAllText(_m3uPath);
			}
			catch
			{
				return null;
			}
		}
		set
		{
			File.WriteAllText(_m3uPath, value);
		}
	}

	public string? Epg
	{
		get
		{
			try
			{
				return File.ReadAllText(_epgPath);
			}
			catch
			{
				return null;
			}
		}
		set
		{
			File.WriteAllText(_epgPath, value);
		}
	}
}
