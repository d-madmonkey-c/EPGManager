using System;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace EPGManager;

internal static class Utility
{

	/// <summary>
	/// Checks if the DateTime is between the provided start and end times (inclusive).
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	internal static bool IsBetween(this DateTime @this, DateTime start, DateTime end)
	{
		return start <= @this && @this <= end;
	}

	/// <summary>
	/// Checks if the DateTime is within the provided start and end times (exclusive).
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <returns></returns>
	internal static bool IsWithin(this DateTime @this, DateTime start, DateTime end)
	{
		return start < @this && @this < end;
	}

	internal static string ComputeFileHash(string v)
	{
		if (!File.Exists(v))
			return string.Empty;

		using var stream = File.OpenRead(v);
		var hash = System.Security.Cryptography.SHA256.HashData(stream);
		return Convert.ToHexString(hash);
	}

	internal static T LoadJson<T>(string path)
	{
		if (!File.Exists(path))
			return default;

		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			WriteIndented = true
		});
	}

	internal static void SaveJson<T>(string path, T data)
	{
		var directory = Path.GetDirectoryName(path);
		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
		{
			WriteIndented = true
		});

		File.WriteAllText(path, json);
	}
}
