using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Infrastructure.Network;

public interface IChatNetworkFactory
{
  IChatNetwork CreateClient(string clientId, string? teamId = null);

  IChatNetwork CreateClient(string clientId, int latencyMs, string? teamId = null);
}