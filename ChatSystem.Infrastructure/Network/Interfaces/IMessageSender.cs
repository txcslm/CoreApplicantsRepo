using ChatSystem.Domain.ValueObjects;

namespace ChatSystem.Infrastructure.Network.Interfaces;

public interface IMessageSender : INetworkClient
{
  Task SendMessageAsync(string message, string sender);

  Task SendMessageAsync(string message, string sender, ChatType chatType, string? teamId = null);
}