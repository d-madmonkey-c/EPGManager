using System.Xml.Linq;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using EPGManager.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<OutputStore>();
builder.Services.AddHostedService<RefreshWorker>();

var app = builder.Build();

app.MapGet("/", async (ConfigStore configStore, HttpResponse res) =>
{
    var config = await configStore.LoadAsync();

    res.ContentType = "text/html; charset=utf-8";
    await res.WriteAsync($"""
    <html>
    <body>
        <h1>EPG Manager</h1>

        <form method="post" action="/config">
            <h2>Source URLs</h2>
            <label>Primary M3U URL:</label><br/>
            <input type="text" name="PrimaryUrl" value="{config.PrimaryUrl}" size="80"/><br/><br/>

            <label>UTC XML URL:</label><br/>
            <input type="text" name="UtcUrl" value="{config.UtcUrl}" size="80"/><br/><br/>

            <label>EPG_CA XML URL:</label><br/>
            <input type="text" name="EpgCaUrl" value="{config.EpgCaUrl}" size="80"/><br/><br/>

            <h2>Recategorization Rules</h2>
            <table border="1" cellpadding="4">
                <tr>
                    <th>tvg-id</th>
                    <th>New Group</th>
                    <th>New Name</th>
                    <th>New Logo</th>
                    <th>Hide?</th>
                </tr>
    """);

    foreach (var rule in config.RecategorizationRules)
    {
        await res.WriteAsync($"""
            <tr>
                <td><input name="MatchTvgId" value="{rule.MatchTvgId}" /></td>
                <td><input name="NewGroupTitle" value="{rule.NewGroupTitle}" /></td>
                <td><input name="NewTvgName" value="{rule.NewTvgName}" /></td>
                <td><input name="NewTvgLogo" value="{rule.NewTvgLogo}" /></td>
                <td><input type="checkbox" name="Hidden" {(rule.Hidden ? "checked" : "")} /></td>
            </tr>
        """);
    }

    await res.WriteAsync("""
            </table>
            <br/>
            <button type="submit">Save</button>
        </form>
    </body>
    </html>
    """);
});

app.MapPost("/config", async (HttpRequest req, ConfigStore configStore, RefreshWorker refreshWorker) =>
{
    var form = await req.ReadFormAsync();
    var config = await configStore.LoadAsync();

    config.PrimaryUrl = form["PrimaryUrl"]!;
    config.UtcUrl = form["UtcUrl"]!;
    config.EpgCaUrl = form["EpgCaUrl"]!;

    config.RecategorizationRules.Clear();

    var ids = form["MatchTvgId"];
    var groups = form["NewGroupTitle"];
    var names = form["NewTvgName"];
    var logos = form["NewTvgLogo"];
    var hidden = form["Hidden"];

    for (int i = 0; i < ids.Count; i++)
    {
        if (string.IsNullOrWhiteSpace(ids[i])) continue;

        config.RecategorizationRules.Add(new RecategorizationRule
        {
            MatchTvgId = ids[i],
            NewGroupTitle = groups[i],
            NewTvgName = names[i],
            NewTvgLogo = logos[i],
            Hidden = hidden.Contains(ids[i])
        });
    }

    await configStore.SaveAsync(config);

    return Results.Redirect("/");
});

app.MapGet("/output/primary", (OutputStore store) =>
{
    if (store.PrimaryOutput == null)
        return Results.NotFound("Primary output not generated yet.");

    return Results.File(
        Encoding.UTF8.GetBytes(store.PrimaryOutput),
        "audio/x-mpegurl",
        "guide.m3u");
});

app.MapGet("/output/secondary", (OutputStore store) =>
{
    if (store.SecondaryOutputXml == null)
        return Results.NotFound("Secondary output not generated yet.");

    return Results.File(
        Encoding.UTF8.GetBytes(store.SecondaryOutputXml),
        "application/xml",
        "epg.xml");
});

app.Run();