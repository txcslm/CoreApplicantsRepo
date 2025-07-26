using ChatSystem.Domain.Entities;

namespace ChatSystem.Infrastructure.Network;

public interface IEventPublisher : INetworkClient
{
  Task RaiseEventAsync(EventType eventType, object data);
}