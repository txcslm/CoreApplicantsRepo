using ChatSystem.Domain.Entities;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure.Network;

namespace ChatSystem.Tests;

[TestFixture]
public class MultiClientTests
{
  [SetUp]
  public void SetUp()
  {
    MockChatNetwork.ClearAllClients();
    _clients.Clear();
  }

  [TearDown]
  public void TearDown()
  {
    MockChatNetwork.ClearAllClients();
  }

  private readonly List<MockChatNetwork> _clients = [];

  [Test]
  public async Task Broadcast_PublicMessage_ToAllClients()
  {
    var client1 = new MockChatNetwork("Client1", 50);
    var client2 = new MockChatNetwork("Client2", 50);
    var client3 = new MockChatNetwork("Client3", 50);

    _clients.AddRange(new[] { client1, client2, client3 });

    List<ChatMessage> receivedMessages = new List<ChatMessage>();

    client2.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));
    client3.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    await client1.SendMessageAsync("Hello everyone!", "Player1", ChatType.Public);
    await Task.Delay(200);

    Assert.That(receivedMessages, Has.Count.EqualTo(2));
    Assert.Multiple(() =>
    {
      Assert.That(receivedMessages.All(m => m.Content == "Hello everyone!"), Is.True);
      Assert.That(receivedMessages.All(m => m.Sender == "Player1"), Is.True);
      Assert.That(receivedMessages.All(m => m.Type == ChatType.Public), Is.True);
    });
  }

  [Test]
  public async Task TeamMessage_OnlyReachesTeamMembers()
  {
    var client1 = new MockChatNetwork("Client1", 50);
    var client2 = new MockChatNetwork("Client2", 50);
    var client3 = new MockChatNetwork("Client3", 50);

    client1.SetTeamId("TeamA");
    client2.SetTeamId("TeamA");
    client3.SetTeamId("TeamB");

    _clients.AddRange([client1, client2, client3]);

    List<ChatMessage> teamAMessages = new List<ChatMessage>();
    List<ChatMessage> teamBMessages = new List<ChatMessage>();

    client2.OnMessageReceived.Subscribe(msg => teamAMessages.Add(msg));
    client3.OnMessageReceived.Subscribe(msg => teamBMessages.Add(msg));

    await client1.SendMessageAsync("Team A strategy", "Player1", ChatType.Team, "TeamA");
    await Task.Delay(200);

    Assert.Multiple(() =>
    {
      Assert.That(teamAMessages, Has.Count.EqualTo(1));
      Assert.That(teamBMessages, Is.Empty);
    });
    Assert.Multiple(() =>
    {
      Assert.That(teamAMessages[0].Content, Is.EqualTo("Team A strategy"));
      Assert.That(teamAMessages[0].Type, Is.EqualTo(ChatType.Team));
    });
  }

  [Test]
  public async Task DisconnectedClient_DoesNotReceiveMessages()
  {
    var client1 = new MockChatNetwork("Client1", 50);
    var client2 = new MockChatNetwork("Client2", 50);

    _clients.AddRange([client1, client2]);

    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    client2.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    client2.SimulateDisconnect();

    await client1.SendMessageAsync("Hello", "Player1", ChatType.Public);
    await Task.Delay(200);

    Assert.That(receivedMessages, Is.Empty);
  }

  [Test]
  public async Task ReconnectedClient_ReceivesMessagesAgain()
  {
    var client1 = new MockChatNetwork("Client1", 50);
    var client2 = new MockChatNetwork("Client2", 50);

    _clients.AddRange(new[] { client1, client2 });

    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    client2.OnMessageReceived.Subscribe(msg => receivedMessages.Add(msg));

    client2.SimulateDisconnect();
    client2.SimulateReconnect();

    await client1.SendMessageAsync("Hello after reconnect", "Player1", ChatType.Public);
    await Task.Delay(200);

    Assert.That(receivedMessages, Has.Count.EqualTo(1));
    Assert.That(receivedMessages[0].Content, Is.EqualTo("Hello after reconnect"));
  }

  [Test]
  public async Task EventBroadcast_ReachesAllConnectedClients()
  {
    var client1 = new MockChatNetwork("Client1", 50);
    var client2 = new MockChatNetwork("Client2", 50);
    var client3 = new MockChatNetwork("Client3", 50);

    _clients.AddRange([client1, client2, client3]);

    List<(EventType, object)> receivedEvents = new List<(EventType, object)>();

    client2.OnEventReceived.Subscribe(evt => receivedEvents.Add(evt));
    client3.OnEventReceived.Subscribe(evt => receivedEvents.Add(evt));

    await client1.RaiseEventAsync(EventType.MatchStart, "Match starting now!");
    await Task.Delay(200);

    Assert.That(receivedEvents, Has.Count.EqualTo(2));
    Assert.Multiple(() =>
    {
      Assert.That(receivedEvents.All(e => e.Item1 == EventType.MatchStart), Is.True);
      Assert.That(receivedEvents.All(e => e.Item2.ToString() == "Match starting now!"), Is.True);
    });
  }
}