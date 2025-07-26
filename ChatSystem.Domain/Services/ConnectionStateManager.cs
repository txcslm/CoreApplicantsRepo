namespace ChatSystem.Domain.Services;

public class ConnectionStateManager : IConnectionStateSubject
{
  private readonly Lock _lock = new Lock();
  private readonly List<IConnectionStateObserver> _observers = new List<IConnectionStateObserver>();

  public void Subscribe(IConnectionStateObserver observer)
  {
    lock (_lock)
    {
      if (!_observers.Contains(observer))
      {
        _observers.Add(observer);
      }
    }
  }

  public void Unsubscribe(IConnectionStateObserver observer)
  {
    lock (_lock)
    {
      _observers.Remove(observer);
    }
  }

  public void NotifyObservers(string clientId, ConnectionState state)
  {
    List<IConnectionStateObserver> observersCopy;
    lock (_lock)
    {
      observersCopy = new List<IConnectionStateObserver>(_observers);
    }

    foreach (var observer in observersCopy)
    {
      try
      {
        observer.OnConnectionStateChanged(clientId, state);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[ConnectionStateManager] Observer error: {ex.Message}");
      }
    }
  }
}