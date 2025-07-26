namespace ChatSystem.Application.Interfaces;

public interface IRequestHandler<in TRequest>
  where TRequest : IRequest
{
  Task Handle(TRequest request);
}

public interface IRequestHandler<in TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  Task<TResponse> Handle(TRequest request);
}