using System.Reflection;
using EPGManager;
using EPGManager.Data;
using NUnit.Framework.Legacy;

namespace EPGManager.Test;

public class ConfigStoreTests
{
    private static readonly SelectedChannelList selectedChannels = new SelectedChannelList
        {
            new SelectedChannel {
                Id = "channel.test.id",
                Name = "Test Channel 1 Custom",
                LogoUri = "http://example.com/logo1_custom.png",
                Uri = "http://example.com/channel1.m3u",
                Groups = new List<string> { "Group A", "Group B" }/*,
                EpgChannelIds = new Dictionary<string, string> {
                    { "source1", "epg_channel_1" },
                    { "source2", "epg_channel_2" }
                }*/
            },
            new SelectedChannel
            {
                Id = "channel.test.otherId",
                Name = "Test Channel 2",
                LogoUri = "http://example.com/logo2.png",
                Uri = "http://example.com/channel2.m3u"
            }
        };

    private static readonly string SelectedChannelsJson = @"[
  {
    ""Id"": ""channel.test.id"",
    ""Name"": ""Test Channel 1 Custom"",
    ""LogoUri"": ""http://example.com/logo1_custom.png"",
    ""Uri"": ""http://example.com/channel1.m3u"",
    ""Groups"": [
      ""Group A"",
      ""Group B""
    ],
    ""EpgChannelIds"": {
      ""source1"": ""epg_channel_1"",
      ""source2"": ""epg_channel_2""
    }
  },
  {
    ""Id"": ""channel.test.otherId"",
    ""Name"": ""Test Channel 2"",
    ""LogoUri"": ""http://example.com/logo2.png"",
    ""Uri"": ""http://example.com/channel2.m3u"",
    ""Groups"": [],
    ""EpgChannelIds"": {}
  }
]";

    [SetUp]
    public void Setup()
    {
        string testPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "selectedchannels_load.json");
        if (File.Exists(testPath))
            File.Delete(testPath);
        File.WriteAllText(testPath, SelectedChannelsJson);
    }

    [Test]
    public void Saves()
    {
        MethodInfo? saveJson = typeof(ConfigStore).GetMethod("SaveJson", BindingFlags.NonPublic | BindingFlags.Static);

        var testMethod = saveJson.MakeGenericMethod(typeof(SelectedChannelList));
        string testPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "selectedchannels_save.json");
        testMethod.Invoke(null, [testPath, selectedChannels]);
        var result = File.ReadAllText(testPath);
        Console.WriteLine("Saved JSON:");
        Console.WriteLine(result);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(SelectedChannelsJson));
    }

    [Test]
    public void Loads()
    {
        MethodInfo? loadJson = typeof(ConfigStore).GetMethod("LoadJson", BindingFlags.NonPublic | BindingFlags.Static);

        var testMethod = loadJson.MakeGenericMethod(typeof(SelectedChannelList));
        string testPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "selectedchannels_load.json");
        var result = testMethod.Invoke(null, [testPath]);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<SelectedChannelList>());
        var typeResult = result as SelectedChannelList;
        Assert.That(typeResult.Count, Is.EqualTo(selectedChannels.Count));
        SelectedChannel resultChannel, selectedChannel;
        for (int i = 0; i < selectedChannels.Count; i++)
        {
            Assert.That(typeResult.Any(c => c.Id == selectedChannels[i].Id));
            resultChannel = typeResult.First(c => c.Id == selectedChannels[i].Id);
            selectedChannel = selectedChannels[i];
            Assert.That(resultChannel.Name, Is.EqualTo(selectedChannel.Name));
            Assert.That(resultChannel.LogoUri, Is.EqualTo(selectedChannel.LogoUri));
            Assert.That(resultChannel.Uri, Is.EqualTo(selectedChannel.Uri));
            Assert.That(resultChannel.Groups, Is.EquivalentTo(selectedChannel.Groups));
            Assert.That(resultChannel.EpgChannelIds, Is.EquivalentTo(selectedChannel.EpgChannelIds));
        }
        //Assert.That(result as SelectedChannelList, Is.EquivalentTo(selectedChannels));
    }
}
