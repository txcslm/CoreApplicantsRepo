namespace ChatSystem.Domain.Services;

public interface IConnectionStateSubject
{
  void Subscribe(IConnectionStateObserver observer);

  void Unsubscribe(IConnectionStateObserver observer);

  void NotifyObservers(string clientId, ConnectionState state);
}