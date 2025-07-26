using ChatSystem.Domain.Services;

namespace ChatSystem.Tests;

[TestFixture]
public class ConnectionStateTests
{
  [SetUp]
  public void Setup()
  {
    _manager = new ConnectionStateManager();
    _observer1 = new TestObserver("Observer1");
    _observer2 = new TestObserver("Observer2");
  }

  private ConnectionStateManager _manager = null!;
  private TestObserver _observer1 = null!;
  private TestObserver _observer2 = null!;

  [Test]
  public void Subscribe_SingleObserver_ReceivesNotifications()
  {
    _manager.Subscribe(_observer1);

    _manager.NotifyObservers("Client1", ConnectionState.Connected);

    Assert.That(_observer1.Notifications.Count, Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(_observer1.Notifications[0].ClientId, Is.EqualTo("Client1"));
            Assert.That(_observer1.Notifications[0].State, Is.EqualTo(ConnectionState.Connected));
        });
    }

  [Test]
  public void Subscribe_MultipleObservers_AllReceiveNotifications()
  {
    _manager.Subscribe(_observer1);
    _manager.Subscribe(_observer2);

    _manager.NotifyObservers("Client1", ConnectionState.Disconnected);

        Assert.Multiple(() =>
        {
            Assert.That(_observer1.Notifications.Count, Is.EqualTo(1));
            Assert.That(_observer2.Notifications.Count, Is.EqualTo(1));

            Assert.That(_observer1.Notifications[0].State, Is.EqualTo(ConnectionState.Disconnected));
        });
        Assert.That(_observer2.Notifications[0].State, Is.EqualTo(ConnectionState.Disconnected));
  }

  [Test]
  public void Subscribe_DuplicateObserver_DoesNotDuplicate()
  {
    _manager.Subscribe(_observer1);
    _manager.Subscribe(_observer1);

    _manager.NotifyObservers("Client1", ConnectionState.Connected);

    Assert.That(_observer1.Notifications, Has.Count.EqualTo(1));
  }

  [Test]
  public void Unsubscribe_RemovesObserver()
  {
    _manager.Subscribe(_observer1);
    _manager.Subscribe(_observer2);

    _manager.Unsubscribe(_observer1);
    _manager.NotifyObservers("Client1", ConnectionState.Connected);

    Assert.That(_observer1.Notifications, Is.Empty);
    Assert.That(_observer2.Notifications, Has.Count.EqualTo(1));
  }

  [Test]
  public void NotifyObservers_WithNoObservers_DoesNotThrow()
  {
    Assert.DoesNotThrow(() => { _manager.NotifyObservers("Client1", ConnectionState.Connected); });
  }

  [Test]
  public void NotifyObservers_WithThrowingObserver_ContinuesWithOthers()
  {
    var throwingObserver = new ThrowingObserver();

    _manager.Subscribe(throwingObserver);
    _manager.Subscribe(_observer1);

    Assert.DoesNotThrow(() => { _manager.NotifyObservers("Client1", ConnectionState.Connected); });

    Assert.That(_observer1.Notifications, Has.Count.EqualTo(1));
  }

  [Test]
  public void ConnectionLogger_LogsStateChanges()
  {
    var logger = new ConnectionLogger();

    Assert.DoesNotThrow(() =>
    {
      logger.OnConnectionStateChanged("TestClient", ConnectionState.Connected);
      logger.OnConnectionStateChanged("TestClient", ConnectionState.Disconnected);
    });
  }

  private class TestObserver : IConnectionStateObserver
  {
    public TestObserver(string name) =>
      Name = name;

    public List<(string ClientId, ConnectionState State)> Notifications { get; } = new List<(string ClientId, ConnectionState State)>();

    public string Name { get; }

    public void OnConnectionStateChanged(string clientId, ConnectionState state)
    {
      Notifications.Add((clientId, state));
    }
  }

  private class ThrowingObserver : IConnectionStateObserver
  {
    public void OnConnectionStateChanged(string clientId, ConnectionState state)
    {
      throw new InvalidOperationException("Test exception");
    }
  }
}