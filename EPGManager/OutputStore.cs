using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EPGManager.Data;

namespace EPGManager;

public class OutputStore
{
	public readonly string _m3uPath = Path.Combine(AppContext.BaseDirectory, "cache", "myguide.m3u");
	public readonly string _epgXmlPath = Path.Combine(AppContext.BaseDirectory, "cache", "myguide.xml");
	public readonly string _epgGzPath = Path.Combine(AppContext.BaseDirectory, "cache", "myguide.xml.gz");
	private static readonly XmlWriterSettings _xmlSettings = new XmlWriterSettings
	{
		Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
		Indent = true,
		OmitXmlDeclaration = false
	};

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
			if (string.IsNullOrEmpty(value))
			{
				try
				{
					File.Delete(_m3uPath);
				}
				catch
				{
					//Do Nothing
				}
			}
			else
			{
				File.WriteAllText(_m3uPath, value);
			}
		}
	}

	public XDocument? Epg
	{
		get
		{
			try
			{
				if (_epg?.Root?.Elements().Count() <= 0) LoadEpg();
				return _epg;
			}
			catch
			{
				return null;
			}
		}
		set
		{
			_epg = value;
			SaveEpg();
		}
	}

	private XDocument? _epg;

	internal DateTime LoadEpg()
	{
		try
		{
			DateTime lastRefresh = File.GetLastWriteTime(_epgXmlPath);
			using FileStream fs = new FileStream(_epgXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			_epg = XDocument.Load(fs);
			return lastRefresh;
		}
		catch
		{
			return DateTime.UnixEpoch;
		}
	}

	private void SaveEpg()
	{
		if ((_epg?.Elements().Count() ?? -1) > 0)
		{
			using (var writer = XmlWriter.Create(_epgXmlPath, _xmlSettings))
				_epg.Save(writer);

			// Gzip the file
			using (var input = File.OpenRead(_epgXmlPath))
			using (var output = File.Create(_epgGzPath))
			using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
				input.CopyTo(gzip);
		}
		else
		{
			try
			{
				File.Delete(_epgXmlPath);
			}
			catch
			{
				//Do Nothing
			}
			try
			{
				File.Delete(_epgGzPath);
			}
			catch
			{
				//Do Nothing
			}
		}
	}

	private static byte[] SerializeUtf8(XDocument doc)
	{
		using var ms = new MemoryStream();
		using var writer = XmlWriter.Create(ms, _xmlSettings);
		doc.Save(writer);

		return ms.ToArray();
	}

	private static byte[] Gzip(byte[] input)
	{
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
			gzip.Write(input, 0, input.Length);

		return output.ToArray();
	}
}
