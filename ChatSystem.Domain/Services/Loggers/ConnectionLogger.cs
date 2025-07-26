namespace ChatSystem.Domain.Services;

public class ConnectionLogger : IConnectionStateObserver
{
  public void OnConnectionStateChanged(string clientId, ConnectionState state)
  {
    Console.WriteLine($"[ConnectionLogger] Client {clientId} state changed to: {state}");
  }
}