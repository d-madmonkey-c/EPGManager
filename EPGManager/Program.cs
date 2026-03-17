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

app.MapGet("/", async (ConfigStore configStore, OutputStore outputStore, HttpResponse res) =>
{
    var config = await configStore.LoadAsync();
    var channels = outputStore.AvailableChannels ?? new List<ChannelIdentity>();

    // Group channels by original group-title
    var grouped = channels
        .GroupBy(c => c.OriginalGroupTitle ?? "Ungrouped")
        .OrderBy(g => g.Key)
        .ToList();

    res.ContentType = "text/html; charset=utf-8";

    await res.WriteAsync(@$"
<html>
<head>
<style>
body {{font - family: Arial;
}}
.panel {{float: left;
    width: 30%;
    height: 80vh;
    overflow-y: auto;
    padding: 10px;
    border-right: 1px solid #ccc;
}}
.channel-item {{padding: 4px;
    cursor: pointer;
}}
.channel-item:hover {{background - color: #eef;
}}
.group-title {{font - weight: bold;
    margin-top: 10px;
    cursor: pointer;
}}
#settings-content input {{width: 90%;
}}
</style>
</head>

<body>
<h1>EPG Manager</h1>

<form method='post' action='/config' onsubmit='beforeSubmit()'>
            <h2>Source URLs</h2>
            <label>Primary M3U URL:</label><br/>
            <input type='text' name='PrimaryUrl' value='{config.PrimaryUrl}' size='80'/><br/><br/>

            < label > UTC XML URL:</ label >< br />
            < input type = 'text' name = 'UtcUrl' value = '{config.UtcUrl}' size = '80' />< br />< br />

            < label > EPG_CA XML URL:</ label >< br />
            < input type = 'text' name = 'EpgCaUrl' value = '{config.EpgCaUrl}' size = '80' />< br />< br />

< div class='panel' id='all-channels'>
    <h2>Available Channels</h2>
");

    // LEFT PANEL — AVAILABLE CHANNELS
    foreach (var group in grouped)
    {
        await res.WriteAsync($"""
        <div class="group">
            <div class="group-title" onclick="toggleGroup('{group.Key}')">{group.Key}</div>
            <div id="group-{group.Key}" style="margin-left: 10px;">
        """);

        foreach (var ch in group)
        {
            await res.WriteAsync($"""
                <div class="channel-item" onclick="addChannel('{ch.M3uTvgId}', '{ch.Name}')">
                    {ch.Name}
                </div>
            """);
        }

        await res.WriteAsync("</div></div>");
    }

    // MIDDLE PANEL — SELECTED CHANNELS
    await res.WriteAsync("""
</div>

<div class="panel" id="selected-channels">
    <h2>Selected Channels</h2>
    <ul id="selected-list">
""");

    foreach (var tvgId in config.SelectedChannels)
    {
        var ch = channels.FirstOrDefault(c => c.M3uTvgId == tvgId);
        if (ch == null) continue;

        await res.WriteAsync($"""
    <li draggable='true' data-id='{tvgId}' onclick='showSettings('{tvgId}')'>{ch.Name}</li>
""");
    }

    await res.WriteAsync(@"
    </ul>
</div>

<div class='panel' id='channel-settings'>
    <h2>Channel Settings</h2>
    <div id='settings-content'>
        <p>Select a channel to edit settings.</p>
    </div>
</div>

<div style='clear: both;'></div>

<input type='hidden' name='SelectedChannels' id='selected-channels-input' />
<input type='hidden' name='RulesJson' id='rules-json-input' />

<button type='submit'>Save</button>

</form>

<script>
let rules = JSON.parse('" + System.Text.Json.JsonSerializer.Serialize(config.RecategorizationRules) + @"');


function toggleGroup(groupName) {
    const el = document.getElementById('group-' + groupName);
    el.style.display = el.style.display === 'none' ? 'block' : 'none';
}

function addChannel(tvgId, name)
{
    const list = document.getElementById('selected-list');

    // Prevent duplicates
    if ([...list.children].some(li => li.dataset.id === tvgId))
        return;

    const li = document.createElement('li');
    li.dataset.id = tvgId;
    li.textContent = name;
    li.onclick = () => showSettings(tvgId);

    list.appendChild(li);
}

function showSettings(tvgId)
{
    const settings = rules[tvgId] || { tvgId: tvgId }
    ;

    document.getElementById('settings-content').innerHTML = `
        < h3 >${ tvgId}</ h3 >
        < label > New Group:</ label >< br />
        < input id = 'rule-group' value = '${settings.newGroupTitle || ''}' >< br />< br />

        < label > New Name:</ label >< br />
        < input id = 'rule-name' value = '${settings.newTvgName || ''}' >< br />< br />

        < label > New Logo:</ label >< br />
        < input id = 'rule-logo' value = '${settings.newTvgLogo || ''}' >< br />< br />

        < label >< input type = 'checkbox' id = 'rule-hidden' ${ settings.hidden ? 'checked' : ''}> Hidden </ label >< br />< br />

        < button type = 'button' onclick = 'saveRule('${tvgId}')' > Save Rule </ button >
    `;
}

function saveRule(tvgId)
{
    rules[tvgId] = {
    tvgId: tvgId,
        newGroupTitle: document.getElementById('rule-group').value,
        newTvgName: document.getElementById('rule-name').value,
        newTvgLogo: document.getElementById('rule-logo').value,
        hidden: document.getElementById('rule-hidden').checked
    }
        ;
    }

    function beforeSubmit()
    {
        const ids = [...document.querySelectorAll('#selected-list li')]
            .map(li => li.dataset.id);

        document.getElementById('selected-channels-input').value = ids.join(',');
        document.getElementById('rules-json-input').value = JSON.stringify(rules);
    }

// --- DRAG AND DROP FOR SELECTED CHANNELS ---

let draggedItem = null;

document.addEventListener('DOMContentLoaded', () => {
    const list = document.getElementById('selected-list');

    list.addEventListener('dragstart', (e) =>
    {
        if (e.target.tagName === 'LI')
        {
            draggedItem = e.target;
            e.dataTransfer.effectAllowed = 'move';
            e.target.style.opacity = '0.4';
        }
    });

    list.addEventListener('dragend', (e) =>
    {
        if (e.target.tagName === 'LI')
        {
            e.target.style.opacity = '1';
        }
    });

    list.addEventListener('dragover', (e) =>
    {
        e.preventDefault();
        const target = e.target.closest('li');
        if (!target || target === draggedItem) return;

        const rect = target.getBoundingClientRect();
        const midpoint = rect.top + rect.height / 2;

        if (e.clientY < midpoint)
        {
            target.parentNode.insertBefore(draggedItem, target);
        }
        else
        {
            target.parentNode.insertBefore(draggedItem, target.nextSibling);
        }
    });
});
</ script >

</ body >
</ html >
");
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