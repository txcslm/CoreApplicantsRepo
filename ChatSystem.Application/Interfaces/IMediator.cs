namespace ChatSystem.Application.Interfaces;

public interface IMediator : IHandlerRegistry, ICommandSender, INotificationPublisher
{
}