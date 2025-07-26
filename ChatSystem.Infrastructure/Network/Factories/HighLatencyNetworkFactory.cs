using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Infrastructure.Network;

public class HighLatencyNetworkFactory : IChatNetworkFactory
{
  public IChatNetwork CreateClient(string clientId, string? teamId = null) =>
    CreateClient(clientId, 1000, teamId);

  public IChatNetwork CreateClient(string clientId, int latencyMs, string? teamId = null)
  {
    var client = new MockChatNetwork(clientId, Math.Max(latencyMs, 800));
    if (!string.IsNullOrEmpty(teamId))
    {
      client.SetTeamId(teamId);
    }
    return client;
  }
}