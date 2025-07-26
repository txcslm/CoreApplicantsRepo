using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;

namespace ChatSystem.Tests;

[TestFixture]
public class MessageProcessorTests
{
  [SetUp]
  public void Setup()
  {
    _publicProcessor = new PublicMessageProcessor();
    _teamProcessor = new TeamMessageProcessor();
  }

  private PublicMessageProcessor _publicProcessor = null!;
  private TeamMessageProcessor _teamProcessor = null!;

  [Test]
  public async Task PublicMessageProcessor_WithValidMessage_ReturnsTrue()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = "Hello everyone!",
      Sender = "Player1",
      Type = ChatType.Public
    };

    bool result = await _publicProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task PublicMessageProcessor_WithEmptyContent_ReturnsFalse()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = "",
      Sender = "Player1",
      Type = ChatType.Public
    };

    bool result = await _publicProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task TeamMessageProcessor_WithValidTeamMessage_ReturnsTrue()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = "Team strategy",
      Sender = "Player1",
      Type = ChatType.Team,
      TeamId = "TeamA"
    };

    bool result = await _teamProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task TeamMessageProcessor_WithEmptySender_ReturnsFalse()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = "Team strategy",
      Sender = "",
      Type = ChatType.Team,
      TeamId = "TeamA"
    };

    bool result = await _teamProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task TeamMessageProcessor_WithNullContent_ReturnsFalse()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = null!,
      Sender = "Player1",
      Type = ChatType.Team,
      TeamId = "TeamA"
    };

    bool result = await _teamProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task MessageProcessor_WithWhitespaceContent_ReturnsFalse()
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = "   ",
      Sender = "Player1",
      Type = ChatType.Public
    };

    bool result = await _publicProcessor.ProcessMessageAsync(message);

    Assert.That(result, Is.False);
  }
}