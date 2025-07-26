using System.Reactive.Subjects;
using ChatSystem.Domain.Entities;

namespace ChatSystem.Infrastructure.Network;

public interface IEventSubscriber : INetworkClient
{
  ISubject<(EventType, object)> OnEventReceived { get; }
}