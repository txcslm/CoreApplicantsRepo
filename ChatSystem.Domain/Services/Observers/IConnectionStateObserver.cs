namespace ChatSystem.Domain.Services;

public interface IConnectionStateObserver
{
  void OnConnectionStateChanged(string clientId, ConnectionState state);
}