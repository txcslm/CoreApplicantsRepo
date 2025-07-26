namespace ChatSystem.Infrastructure.Network;

public interface ITeamMember : INetworkClient
{
  void SetTeamId(string? teamId);
}