using ChatSystem.Application;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Services;
using ChatSystem.Presentation;
using ChatSystem.Presentation.Services;

namespace ChatSystem.Tests;

[TestFixture]
public class IntegrationTests
{
  [SetUp]
  public void Setup()
  {
    var services = new ServiceCollection();
    services.AddApplicationServices();
    services.AddMockNetworkInfrastructure("TestClient", 50);
    services.AddPresentationServices();

    _serviceProvider = services.BuildServiceProvider();
    _chatManager = _serviceProvider.GetRequiredService<ChatManager>();
    _networkFactory = _serviceProvider.GetRequiredService<IChatNetworkFactory>();
  }

  [TearDown]
  public void TearDown()
  {
    _serviceProvider?.Dispose();
  }

  private ServiceProvider? _serviceProvider;
  private ChatManager? _chatManager;
  private IChatNetworkFactory? _networkFactory;

  [Test]
  public async Task ChatManager_WithRetryStrategy_HandlesFailuresGracefully()
  {
    bool messageReceived = false;
    _chatManager!.Messages.Subscribe(_ => messageReceived = true);

    await _chatManager.SendChatMessageAsync("Test message", "TestSender");
    await Task.Delay(100);

    Assert.That(messageReceived, Is.True, "Message should be processed through retry strategy");
  }

  [Test]
  public Task NetworkFactory_CreatesClientsWithCorrectConfiguration()
  {
    var client1 = _networkFactory!.CreateClient("TestClient1", 100, "TeamA");
    var client2 = _networkFactory.CreateClient("TestClient2", 200, "TeamB");

    Assert.Multiple(() =>
    {
      Assert.That(client1.ClientId, Is.EqualTo("TestClient1"));
      Assert.That(client2.ClientId, Is.EqualTo("TestClient2"));
    });

    return Task.CompletedTask;
  }

  [Test]
  public async Task MessageProcessors_FilterMessagesCorrectly()
  {
    var client1 = _networkFactory!.CreateClient("Player1", 50, "TeamA");
    var client2 = _networkFactory.CreateClient("Player2", 50, "TeamA");
    var client3 = _networkFactory.CreateClient("Player3", 50, "TeamB");

    List<ChatMessage> player1Messages = new List<ChatMessage>();
    List<ChatMessage> player2Messages = new List<ChatMessage>();
    List<ChatMessage> player3Messages = new List<ChatMessage>();

    client1.OnMessageReceived.Subscribe(player1Messages.Add);
    client2.OnMessageReceived.Subscribe(player2Messages.Add);
    client3.OnMessageReceived.Subscribe(player3Messages.Add);

    await client1.SendMessageAsync("Team A message", "Player1", ChatType.Team, "TeamA");
    await Task.Delay(100);

        Assert.Multiple(() =>
        {
            Assert.That(player1Messages.Count, Is.EqualTo(1), "Player1 should receive team message");
            Assert.That(player2Messages.Count, Is.EqualTo(1), "Player2 should receive team message");
            Assert.That(player3Messages.Count, Is.EqualTo(0), "Player3 should not receive team message");
        });
    }

  [Test]
  public Task ConnectionStateObservers_NotifyOnStateChanges()
  {
    var connectionStateManager = _serviceProvider!.GetRequiredService<IConnectionStateSubject>();
    List<(string, ConnectionState)> stateChanges = new List<(string, ConnectionState)>();

    connectionStateManager.Subscribe(new TestConnectionObserver(stateChanges));

    connectionStateManager.NotifyObservers("TestClient", ConnectionState.Connected);
    connectionStateManager.NotifyObservers("TestClient", ConnectionState.Disconnected);

    Assert.That(stateChanges, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(stateChanges[0].Item2, Is.EqualTo(ConnectionState.Connected));
            Assert.That(stateChanges[1].Item2, Is.EqualTo(ConnectionState.Disconnected));
        });
        return Task.CompletedTask;
  }

  [Test]
  public void RetryStrategy_IsRegisteredAndAvailable()
  {
    var retryStrategy = _serviceProvider!.GetRequiredService<IRetryStrategy>();
    Assert.That(retryStrategy, Is.Not.Null);
    Assert.That(retryStrategy, Is.TypeOf<ConnectionAwareRetryStrategy>());
  }

  [Test]
  public void MessageProcessors_AreRegisteredAndAvailable()
  {
    var publicProcessor = _serviceProvider!.GetRequiredService<PublicMessageProcessor>();
    var teamProcessor = _serviceProvider!.GetRequiredService<TeamMessageProcessor>();

        Assert.Multiple(() =>
        {
            Assert.That(publicProcessor, Is.Not.Null);
            Assert.That(teamProcessor, Is.Not.Null);
        });
    }

  private class TestConnectionObserver(List<(string, ConnectionState)> stateChanges) : IConnectionStateObserver
  {
    public void OnConnectionStateChanged(string clientId, ConnectionState state)
    {
      stateChanges.Add((clientId, state));
    }
  }
}