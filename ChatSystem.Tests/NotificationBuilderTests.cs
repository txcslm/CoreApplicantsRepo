using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;

namespace ChatSystem.Tests;

[TestFixture]
public class NotificationBuilderTests
{
  [Test]
  public void Build_WithAllProperties_ReturnsCorrectNotification()
  {
    var builder = new NotificationBuilder()
      .SetType(EventType.KillNotification)
      .SetMessage("Player killed")
      .AddMetadata("weapon", "sword")
      .AddMetadata("location", "jungle");

    var result = builder.Build();

    Assert.Multiple(() =>
    {
      Assert.That(result.Type, Is.EqualTo(EventType.KillNotification));
      Assert.That(result.Message, Is.EqualTo("Player killed"));
      Assert.That(result.Metadata["weapon"], Is.EqualTo("sword"));
      Assert.That(result.Metadata["location"], Is.EqualTo("jungle"));
    });
  }

  [Test]
  public void BuildLegacy_ReturnsCompatibleTuple()
  {
    var builder = new NotificationBuilder()
      .SetType(EventType.MatchStart)
      .SetMessage("Game starting");

    var result = builder.BuildLegacy();

    Assert.Multiple(() =>
    {
      Assert.That(result.Item1, Is.EqualTo(EventType.MatchStart));
      Assert.That(result.Item2, Is.EqualTo("Game starting"));
    });
  }

  [Test]
  public void ForKill_CreatesKillNotification()
  {
    var notification = NotificationBuilder.ForKill("Alice", "Bob").Build();

    Assert.Multiple(() =>
    {
      Assert.That(notification.Type, Is.EqualTo(EventType.KillNotification));
      Assert.That(notification.Message, Is.EqualTo("Alice killed Bob"));
      Assert.That(notification.Metadata["killer"], Is.EqualTo("Alice"));
      Assert.That(notification.Metadata["victim"], Is.EqualTo("Bob"));
    });
  }

  [Test]
  public void ForMatchStart_CreatesMatchStartNotification()
  {
    var notification = NotificationBuilder.ForMatchStart().Build();

    Assert.Multiple(() =>
    {
      Assert.That(notification.Type, Is.EqualTo(EventType.MatchStart));
      Assert.That(notification.Message, Is.EqualTo("Match is starting!"));
    });
  }

  [Test]
  public void AddMetadata_WithMultipleValues_StoresAllValues()
  {
    var builder = new NotificationBuilder();

    var result = builder
      .AddMetadata("key1", "value1")
      .AddMetadata("key2", 42)
      .AddMetadata("key3", true)
      .Build();

    Assert.That(result.Metadata, Has.Count.EqualTo(3));
    Assert.Multiple(() =>
    {
      Assert.That(result.Metadata["key1"], Is.EqualTo("value1"));
      Assert.That(result.Metadata["key2"], Is.EqualTo(42));
      Assert.That(result.Metadata["key3"], Is.EqualTo(true));
    });
  }

  [Test]
  public void AddMetadata_WithDuplicateKey_OverwritesValue()
  {
    var builder = new NotificationBuilder();

    var result = builder
      .AddMetadata("key", "value1")
      .AddMetadata("key", "value2")
      .Build();

    Assert.That(result.Metadata["key"], Is.EqualTo("value2"));
  }
}