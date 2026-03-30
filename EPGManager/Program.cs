using System.Xml.Linq;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using EPGManager;
using EPGManager.Data;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<CacheStore>();
builder.Services.AddSingleton<OutputStore>();
builder.Services.AddSingleton<Processor>();
builder.Services.AddSingleton<RefreshWorker>();
builder.Services.AddHostedService<RefreshWorker>();

var app = builder.Build();

// Load config and caches on startup
var configStore = app.Services.GetRequiredService<ConfigStore>();
configStore.LoadAll();
var cacheStore = app.Services.GetRequiredService<CacheStore>();
cacheStore.LoadAll(configStore.SourceConfig.EpgUrls.Select(u => u.Id).ToList());
app.UseStaticFiles();

app.MapGet("/", async (ConfigStore configStore, CacheStore cacheStore, OutputStore outputStore, HttpResponse res) =>
{
    /*↕►▼→←Ξ‡⁞≡*/
    //var config = await configStore.LoadAsync();
    var config = configStore.SourceConfig;
    var channels = cacheStore.Channels;
    var newChannelIds = cacheStore.NewChannelIds ?? [];
    var selectedChannels = configStore.SelectedChannels;

    // Build EPG URL list HTML
    var sources = new List<(string Name, string Url, int Priority)>();
    sources.AddRange(config.EpgUrls.Select(s => (s.Name, s.Url, s.Priority)));
    sources = sources.OrderBy(s => s.Priority).ToList();  // Sort by priority
    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "epgurl.partial.html");
    string epgHtml = File.ReadAllText(templatePath);
    var urlListHtml = string.Join("", config.EpgUrls
        .OrderBy(eu=>eu.Priority)
        .ThenBy(eu=>eu.Name)
        .Select(eu => string.Format(epgHtml, eu.Id, eu.Priority, eu.Name, eu.Url)));

    // Build Available Channels
    // Group channels by original group-title
    var grouped = channels
        .GroupBy(c => c.Group ?? "Ungrouped")
        .OrderBy(g => g.Key)
        .ToList();
    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "availablegroup.partial.html");
    string groupHtml = File.ReadAllText(templatePath);
    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "availablechannel.partial.html");
    string channelHtml = File.ReadAllText(templatePath);
    var availableChannelsHtml = string.Join("", grouped.Select(g => string.Format(groupHtml, g.Key, string.Join("", g.Select(c =>
    {
        var channelItemHtml = string.Format(channelHtml, c.Id, c.Name, c.Group ?? "", c.LogoUri ?? "", c.Uri);
        if (newChannelIds.Contains(c.Id))
        {
            // Add new-channel class to the channel-item div
            channelItemHtml = channelItemHtml.Replace("class='channel-item'", "class='channel-item new-channel'");
        }
        return channelItemHtml;
    })))));

    /*
    var selectedGroupEntries = new List<(string GroupName, Channel Channel, SelectedChannelList Config)>();
    var selectedChannelsById = channels.ToDictionary(c => c.Id, c => c);

    foreach (var selectedConfig in configStore.SelectedChannels)
    {
        if (!selectedChannelsById.TryGetValue(selectedConfig.Id, out var baseChannel))
            continue;

        var groupTitles = selectedConfig.Groups?
            .Select(g => g.Trim())
            .Where(g => !string.IsNullOrEmpty(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!groupTitles.Any())
            groupTitles.Add("Ungrouped");

        foreach (var group in groupTitles)
        {
            / *var channelCopy = new Channel
            {
                Id = baseChannel.Id,
                Name = selectedConfig.EffectiveName,
                Group = group,
                OriginalTvgName = baseChannel.OriginalTvgName,
                LogoUri = baseChannel.LogoUri,
                SecondaryChannelIds = baseChannel.SecondaryChannelIds
            };

            if (!selectedGroupEntries.Any(e => e.GroupName.Equals(group, StringComparison.OrdinalIgnoreCase) && e.Channel.Id == channelCopy.Id))
            {
                selectedGroupEntries.Add((group, channelCopy, selectedConfig));
            }* /
        }
    }
    */

    /*var orderedGroupNames = config.GroupOrder
        .Where(g => selectedGroupEntries.Any(e => e.GroupName.Equals(g, StringComparison.OrdinalIgnoreCase)))
        .Concat(selectedGroupEntries.Select(e => e.GroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Except(config.GroupOrder, StringComparer.OrdinalIgnoreCase))
        .ToList();*/

    // templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "selectedgroup.partial.html");
    // string selectedGroupHtml = File.ReadAllText(templatePath);
    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "selectedchannel.partial.html");
    channelHtml = File.ReadAllText(templatePath);

    /*
    var selectedChannelsHtml = string.Join("", orderedGroupNames.Select(groupName =>
    {
        var groupChannels = selectedGroupEntries
            .Where(e => e.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var channelHtmlContent = string.Join("", groupChannels.Select(e =>
        {
            var cfg = e.Config;
            var epgIdsJson = System.Text.Json.JsonSerializer.Serialize(cfg.EpgChannelIds);
            var groupTitlesJson = System.Text.Json.JsonSerializer.Serialize(cfg.GroupTitles);

            return string.Format(channelHtml, e.Channel.Id, e.Channel.Name)
                .Replace("<div class='channel-item'", $"<div class='channel-item' data-original-name=\"{cfg.OriginalName}\" data-original-group-title=\"{cfg.OriginalGroupTitle}\" data-original-tvg-name=\"{cfg.OriginalTvgName}\" data-original-tvg-logo=\"{cfg.OriginalTvgLogo}\" data-override-group-title=\"{cfg.OverrideGroupTitle}\" data-override-tvg-name=\"{cfg.OverrideTvgName}\" data-override-tvg-logo=\"{cfg.OverrideTvgLogo}\" data-hidden=\"{cfg.Hidden}\" data-epg-channel-ids=\"{epgIdsJson}\" data-group-titles=\"{groupTitlesJson}\"");
        }));

        return string.Format(selectedGroupHtml, groupName, channelHtmlContent);
    }));
    */
    var selectedChannelsHtml = string.Join("", selectedChannels.Select(c => string.Format(channelHtml, c.Id, c.Name, c.LogoUri, c.Uri, string.Join(", ", c.Groups), HtmlEncoder.Default.Encode(JsonSerializer.Serialize(c.EpgChannelIds)))));

    // Serialize rules
    //var rulesDict = config.RecategorizationRules.ToDictionary(r => r.MatchTvgId, r => new { r.NewGroupTitle, r.NewTvgName, r.NewTvgLogo, r.Hidden });
    //var rulesJson = System.Text.Json.JsonSerializer.Serialize(rulesDict);
    var rulesJson = "{}"; // Placeholder since rules are currently disabled

    // Buiild final HTML
    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
    string html = File.ReadAllText(templatePath);
    var styleHash = Utility.ComputeFileHash(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "styles.css"));
    var scriptHash = Utility.ComputeFileHash(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "scripts.js"));
    html = string.Format(html, styleHash, scriptHash, config.M3uUrl, urlListHtml, availableChannelsHtml, selectedChannelsHtml, rulesJson);

    res.ContentType = "text/html; charset=utf-8";
    await res.WriteAsync(html);
});

app.MapPost("/config", async (HttpRequest req, ConfigStore configStore, RefreshWorker refreshWorker) =>
{
    var form = await req.ReadFormAsync();
    //var config = await configStore.LoadAsync();
    //configStore.LoadAll();
    var config = configStore.SourceConfig;

    // M3U URL
    config.M3uUrl = form["PrimaryUrl"]!;

    // EPG URLs
    /*
    var urls = form["urls"].ToList();
    var names = form["names"].ToList();
    var priorities = form["priorities"].Select(p => int.Parse(p)).ToList();
    config.EpgUrls.Clear();
    */
    var epgUrlsJson = form["EpgUrls"];
    if (!string.IsNullOrEmpty(epgUrlsJson))
    {
        var epgUrls = JsonSerializer.Deserialize<List<EpgSource>>(epgUrlsJson);

        for (int i = 0; i < epgUrls.Count; i++)
        {
            // Generate a unique ID for this source
            var epgUrl = epgUrls[i];
            epgUrl.Id ??= $"user_{DateTime.UtcNow.Ticks}_{i}";
            if (config.EpgUrls.Any(eu => eu.Id == epgUrl.Id))
            {
                var target = config.EpgUrls.First(eu => eu.Id == epgUrl.Id);
                target.Priority = epgUrl.Priority;
                target.Name = epgUrl.Name;
                target.Url = epgUrl.Url;
            }
            else
            {
                config.EpgUrls.Add(epgUrl);
            }
        }
        config.EpgUrls.RemoveAll(old => !epgUrls.Any(eu => eu.Id == old.Id));
    }
    //config.RecategorizationRules.Clear();
    /*
    var rulesJson = form["RulesJson"];
    if (!string.IsNullOrEmpty(rulesJson))
    {
        var rulesDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(rulesJson);
        foreach (var kvp in rulesDict)
        {
            / *config.RecategorizationRules.Add(new RecategorizationRule
            {
                MatchTvgId = kvp.Key,
                NewGroupTitle = kvp.Value.GetProperty("newGroupTitle").GetString(),
                NewTvgName = kvp.Value.GetProperty("newTvgName").GetString(),
                NewTvgLogo = kvp.Value.GetProperty("newTvgLogo").GetString(),
                Hidden = kvp.Value.GetProperty("hidden").GetBoolean()
            });* /
        }
    }
    */

    // Process selected channels
    var selectedChannelsJson = form["SelectedChannels"];
    if (!string.IsNullOrEmpty(selectedChannelsJson))
    {
        var selectedChannels = JsonSerializer.Deserialize<List<SelectedChannel>>(selectedChannelsJson);
        if (selectedChannels != null)
        {
            configStore.SelectedChannels.Clear();
            configStore.SelectedChannels.AddRange(selectedChannels);
        }
    }

    //await configStore.SaveAsync(config);
    configStore.SaveAll();
    //await refreshWorker.TriggerRefreshAsync();
    return Results.Redirect("/");
});

app.MapPost("/refresh-m3u", async (Processor processor, CacheStore cacheStore) =>
{
    try
    {
        await processor.RefreshM3uAsync();
        var newChannelCount = cacheStore.NewChannelIds.Count;
        return Results.Ok(new { success = true, message = $"M3U refreshed. Found {newChannelCount} new channels." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

app.MapPost("/refresh-epg", async (Processor processor, CacheStore cacheStore) =>
{
    try
    {
        await processor.RefreshEpgAsync();
        return Results.Ok(new { success = true, message = $"EPG refreshed. Found {cacheStore.EpgChannels.Count} available channel IDs." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
});

app.MapGet("/api/list-epgSources", async (ConfigStore configStore, CacheStore cacheStore) =>
{
    var sources = configStore.SourceConfig.EpgUrls.Select(
        s => new
        {
            id = s.Id,
            name = s.Name,
            channelCount = cacheStore.EpgChannels[s.Id].Count()
        }
    ).ToList();
    return Results.Ok(sources);
});

app.MapGet("/api/list-epgChannels/{sourceId}", async (ConfigStore configStore, CacheStore cacheStore, string sourceId) =>
{
    if (!cacheStore.EpgChannels.ContainsKey(sourceId))
    {
        return Results.NotFound($"No EPG data found for source ID: {sourceId}");
    }
    var channels = cacheStore.EpgChannels[sourceId].Select(
        c => new
        {
            id = c.Id,
            name = c.Name
        }
    ).ToList().OrderBy(c=>c.name);
    return Results.Ok(channels);
});

app.MapGet("/output/m3u", (OutputStore store, Processor processor) =>
{
    var content = store.M3u;
    if (content == null)
    {
        processor.GenerateOutputs();
        content = store.M3u;
        if (content == null)
            return Results.NotFound("Unable to load or generate content.");
    }

    return Results.File(
        Encoding.UTF8.GetBytes(content),
        "audio/x-mpegurl",
        "myguide.m3u"
    );
});

/*
app.MapGet("/output/secondary", (OutputStore store) =>
{
    if (store.SecondaryOutputXml == null)
        return Results.NotFound("Secondary output not generated yet.");

    return Results.File(
        Encoding.UTF8.GetBytes(store.SecondaryOutputXml),
        "application/xml",
        "epg.xml");
});
*/
/*
app.MapPost("/refresh", async (RefreshWorker refreshWorker) =>
{
    await refreshWorker.TriggerRefreshAsync();
    return Results.Ok("Refresh triggered successfully.");
});
*/

/*
*/

app.MapGet("/api/epg-channels", async (ConfigStore configStore, CacheStore cacheStore) =>
{
    //var config = await configStore.LoadAsync();
    var epgSources = new List<object>();

    foreach (var source in configStore.SourceConfig.EpgUrls)
    {
        try
        {
            var channels = new List<object>();

            // Try to load cached EPG data via ID or fallback to name-based cache
            XDocument? doc = null;
            if (!string.IsNullOrEmpty(source.Id))
            {
                doc = await cacheStore.LoadEpgCacheAsync(source.Id);
            }
            if (doc == null && !string.IsNullOrEmpty(source.Name))
            {
                doc = await cacheStore.LoadEpgCacheAsync(source.Name);
            }

            if (doc != null)
            {
                channels = doc.Root?
                    .Elements("channel")
                    .Select(ch => new {
                        id = (string)ch.Attribute("id")!,
                        name = (string)ch.Element("display-name")!
                    })
                    .Where(ch => !string.IsNullOrEmpty(ch.id) && !string.IsNullOrEmpty(ch.name))
                    .OrderBy(ch => ch.name)
                    .Cast<object>()
                    .ToList() ?? new List<object>();
            }

            epgSources.Add(new {
                id = source.Id,
                name = source.Name,
                url = source.Url,
                channels = channels
            });
        }
        catch
        {
            // Still include sources even if they error
            epgSources.Add(new
            {
                id = source.Id,
                name = source.Name,
                url = source.Url,
                channels = new List<object>()
            }
            );
        }
    }
    return Results.Ok(epgSources);
});

app.Run();