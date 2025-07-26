using ChatSystem.Application.Interfaces;

namespace ChatSystem.Application;

public class Mediator : IMediator
{
  private readonly Dictionary<Type, List<object>> _notificationHandlers = new Dictionary<Type, List<object>>();
  private readonly Dictionary<Type, object> _requestHandlers = new Dictionary<Type, object>();
  private readonly Dictionary<Type, Func<IRequest, Task>> _voidRequestHandlers = new Dictionary<Type, Func<IRequest, Task>>();

  public void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
    where TRequest : IRequest<TResponse>
  {
    _requestHandlers[typeof(TRequest)] = handler;
  }

  public void Subscribe<TNotification>(INotificationHandler<TNotification> handler)
    where TNotification : INotification
  {
    var notificationType = typeof(TNotification);

    if (!_notificationHandlers.ContainsKey(notificationType))
    {
      _notificationHandlers[notificationType] = new List<object>();
    }

    _notificationHandlers[notificationType].Add(handler);
  }

  public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
  {
    var requestType = request.GetType();

    if (!_requestHandlers.TryGetValue(requestType, out object? handler))
      throw new InvalidOperationException($"No handler registered for {requestType.Name}");

    var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
    var handleMethod = handlerType.GetMethod("Handle");

    if (handleMethod == null)
      throw new InvalidOperationException($"No handler registered for {requestType.Name}");

    Task<TResponse> task = (Task<TResponse>)handleMethod.Invoke(handler, [request])!;

    return await task;
  }

  public void RegisterHandler<TRequest>(IRequestHandler<TRequest> handler)
    where TRequest : IRequest
  {
    _voidRequestHandlers[typeof(TRequest)] = req => handler.Handle((TRequest)req);
  }

  public async Task Send(IRequest request)
  {
    var requestType = request.GetType();

    if (!_voidRequestHandlers.TryGetValue(requestType, out Func<IRequest, Task>? handlerFunc))
      throw new InvalidOperationException($"No handler registered for {requestType.Name}");

    await handlerFunc(request);
  }

  public async Task Publish<TNotification>(TNotification notification) where TNotification : INotification
  {
    var notificationType = typeof(TNotification);

    if (_notificationHandlers.TryGetValue(notificationType, out List<object>? handlers))
    {
      List<Task> tasks = new List<Task>();

      foreach (object handler in handlers)
      {
        INotificationHandler<TNotification> typedHandler = (INotificationHandler<TNotification>)handler;
        tasks.Add(typedHandler.Handle(notification));
      }

      await Task.WhenAll(tasks);
    }
  }
}