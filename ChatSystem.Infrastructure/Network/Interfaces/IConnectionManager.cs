namespace ChatSystem.Infrastructure.Network.Interfaces;

public interface IConnectionManager : INetworkClient
{
  void SimulateDisconnect();

  void SimulateReconnect();
}