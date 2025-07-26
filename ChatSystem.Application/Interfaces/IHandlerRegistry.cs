namespace ChatSystem.Application.Interfaces;

public interface IHandlerRegistry
{
  void RegisterHandler<TRequest>(IRequestHandler<TRequest> handler) where TRequest : IRequest;

  void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
    where TRequest : IRequest<TResponse>;

  void Subscribe<TNotification>(INotificationHandler<TNotification> handler)
    where TNotification : INotification;
}