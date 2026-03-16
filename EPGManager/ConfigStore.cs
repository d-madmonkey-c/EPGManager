namespace EPGManager.API;

public class ConfigStore
{
	private readonly string _path = Path.Combine(AppContext.BaseDirectory, "config.json");

	public async Task<SourceConfig> LoadAsync()
	{
		if (!File.Exists(_path))
			return new SourceConfig();

		var json = await File.ReadAllTextAsync(_path);
		return System.Text.Json.JsonSerializer.Deserialize<SourceConfig>(json)
			   ?? new SourceConfig();
	}

	public async Task SaveAsync(SourceConfig config)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		});
		await File.WriteAllTextAsync(_path, json);
	}
}