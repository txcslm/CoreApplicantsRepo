using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Infrastructure.Network;

public class MockChatNetworkFactory : IChatNetworkFactory
{
  public IChatNetwork CreateClient(string clientId, string? teamId = null) =>
    CreateClient(clientId, 200, teamId);

  public IChatNetwork CreateClient(string clientId, int latencyMs, string? teamId = null)
  {
    var client = new MockChatNetwork(clientId, latencyMs);
    if (!string.IsNullOrEmpty(teamId))
    {
      client.SetTeamId(teamId);
    }
    return client;
  }
}