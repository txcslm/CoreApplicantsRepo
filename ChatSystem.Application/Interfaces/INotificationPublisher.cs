namespace ChatSystem.Application.Interfaces;

public interface INotificationPublisher
{
  Task Publish<TNotification>(TNotification notification)
    where TNotification : INotification;
}