using ChatSystem.Domain.Entities;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure.Network;

namespace ChatSystem.Tests;

[TestFixture]
public class MockChatNetworkTests
{
  [SetUp]
  public void Setup()
  {
    MockChatNetwork.ClearAllClients();
  }

  [Test]
  public async Task SendMessageAsync_CreatesUniqueMessageId()
  {
    var network = new MockChatNetwork("client1");
    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    network.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    await network.SendMessageAsync("Hello", "User1");

    Assert.That(receivedMessages, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(receivedMessages[0].Id, Is.Not.Null.And.Not.Empty);
            Assert.That(receivedMessages[0].Content, Is.EqualTo("Hello"));
            Assert.That(receivedMessages[0].Sender, Is.EqualTo("User1"));
        });
    }

  [Test]
  public async Task BroadcastMessage_DeliversToMultipleClients()
  {
    var client1 = new MockChatNetwork("client1");
    var client2 = new MockChatNetwork("client2");
    var client3 = new MockChatNetwork("client3");

    List<ChatMessage> messages1 = new List<ChatMessage>();
    List<ChatMessage> messages2 = new List<ChatMessage>();
    List<ChatMessage> messages3 = new List<ChatMessage>();

    client1.OnMessageReceived.Subscribe(msg => messages1.Add(msg));
    client2.OnMessageReceived.Subscribe(msg => messages2.Add(msg));
    client3.OnMessageReceived.Subscribe(msg => messages3.Add(msg));

    await client1.SendMessageAsync("Hello everyone", "User1");

    Assert.Multiple(() =>
    {
      Assert.That(messages1, Has.Count.EqualTo(1));
      Assert.That(messages2, Has.Count.EqualTo(1));
      Assert.That(messages3, Has.Count.EqualTo(1));
      Assert.That(messages1[0].Content, Is.EqualTo("Hello everyone"));
      Assert.That(messages2[0].Content, Is.EqualTo("Hello everyone"));
      Assert.That(messages3[0].Content, Is.EqualTo("Hello everyone"));
    });
  }

  [Test]
  public async Task TeamChat_OnlyDeliversToTeamMembers()
  {
    var teamA1 = new MockChatNetwork("teamA1");
    var teamA2 = new MockChatNetwork("teamA2");
    var teamB1 = new MockChatNetwork("teamB1");

    teamA1.SetTeamId("TeamA");
    teamA2.SetTeamId("TeamA");
    teamB1.SetTeamId("TeamB");

    List<ChatMessage> messagesA1 = new List<ChatMessage>();
    List<ChatMessage> messagesA2 = new List<ChatMessage>();
    List<ChatMessage> messagesB1 = new List<ChatMessage>();

    teamA1.OnMessageReceived.Subscribe(msg => messagesA1.Add(msg));
    teamA2.OnMessageReceived.Subscribe(msg => messagesA2.Add(msg));
    teamB1.OnMessageReceived.Subscribe(msg => messagesB1.Add(msg));

    await teamA1.SendMessageAsync("Team A strategy", "PlayerA", ChatType.Team, "TeamA");

    Assert.Multiple(() =>
    {
      Assert.That(messagesA1, Has.Count.EqualTo(1));
      Assert.That(messagesA2, Has.Count.EqualTo(1));
      Assert.That(messagesB1, Has.Count.EqualTo(0), "TeamB member should not receive TeamA message");
    });
  }

  [Test]
  public async Task RaiseEventAsync_DeliversToAllConnectedClients()
  {
    var client1 = new MockChatNetwork("client1");
    var client2 = new MockChatNetwork("client2");

    List<(EventType, object)> events1 = new List<(EventType, object)>();
    List<(EventType, object)> events2 = new List<(EventType, object)>();

    client1.OnEventReceived.Subscribe(evt => events1.Add(evt));
    client2.OnEventReceived.Subscribe(evt => events2.Add(evt));

    await client1.RaiseEventAsync(EventType.MatchStart, "Game starting!");

    Assert.Multiple(() =>
    {
      Assert.That(events1, Has.Count.EqualTo(1));
      Assert.That(events2, Has.Count.EqualTo(1));
      Assert.That(events1[0].Item1, Is.EqualTo(EventType.MatchStart));
      Assert.That(events1[0].Item2, Is.EqualTo("Game starting!"));
    });
  }

  [Test]
  public async Task DisconnectedClient_DoesNotReceiveMessages()
  {
    var sender = new MockChatNetwork("sender");
    var receiver = new MockChatNetwork("receiver");

    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    receiver.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    receiver.SimulateDisconnect();
    await sender.SendMessageAsync("Should not receive", "Sender");

    Assert.That(receivedMessages, Has.Count.EqualTo(0));
  }

  [Test]
  public async Task ReconnectedClient_ReceivesNewMessages()
  {
    var sender = new MockChatNetwork("sender");
    var receiver = new MockChatNetwork("receiver");

    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    receiver.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    receiver.SimulateDisconnect();
    await sender.SendMessageAsync("Missed message", "Sender");

    receiver.SimulateReconnect();
    await sender.SendMessageAsync("Should receive", "Sender");

    Assert.Multiple(() =>
    {
      Assert.That(receivedMessages, Has.Count.EqualTo(1));
      Assert.That(receivedMessages[0].Content, Is.EqualTo("Should receive"));
    });
  }

  [Test]
  public void GetAllClients_ReturnsCurrentClients()
  {
    var client1 = new MockChatNetwork("client1");
    var client2 = new MockChatNetwork("client2");

    IReadOnlyList<MockChatNetwork> allClients = MockChatNetwork.GetAllClients();

    Assert.Multiple(() =>
    {
      Assert.That(allClients, Has.Count.EqualTo(2));
      Assert.That(allClients.Select(c => c.ClientId), Contains.Item("client1"));
      Assert.That(allClients.Select(c => c.ClientId), Contains.Item("client2"));
    });
  }

  [Test]
  public Task DisconnectedClient_ThrowsExceptionOnSend()
  {
    var network = new MockChatNetwork("client1");
    network.SimulateDisconnect();

    var ex = Assert.ThrowsAsync<Exception>(async () =>
      await network.SendMessageAsync("Test", "User"));

    Assert.That(ex.Message, Is.EqualTo("Disconnected"));

    return Task.CompletedTask;
  }

  [Test]
  public Task DisconnectedClient_ThrowsExceptionOnRaiseEvent()
  {
    var network = new MockChatNetwork("client1");
    network.SimulateDisconnect();

    var ex = Assert.ThrowsAsync<Exception>(async () =>
      await network.RaiseEventAsync(EventType.MatchStart, "data"));

    Assert.That(ex.Message, Is.EqualTo("Disconnected"));

    return Task.CompletedTask;
  }
}