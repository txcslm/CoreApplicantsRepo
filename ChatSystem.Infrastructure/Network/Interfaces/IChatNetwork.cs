namespace ChatSystem.Infrastructure.Network.Interfaces;

public interface IChatNetwork : IMessageSender, IMessageReceiver,
  IEventPublisher, IEventSubscriber, IConnectionManager, ITeamMember
{
}