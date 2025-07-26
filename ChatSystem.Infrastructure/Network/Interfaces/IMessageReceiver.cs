using System.Reactive.Subjects;
using ChatSystem.Domain.Entities;

namespace ChatSystem.Infrastructure.Network;

public interface IMessageReceiver : INetworkClient
{
  ISubject<ChatMessage> OnMessageReceived { get; }
}