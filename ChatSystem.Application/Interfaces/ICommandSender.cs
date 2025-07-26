namespace ChatSystem.Application.Interfaces;

public interface ICommandSender
{
  Task<TResponse> Send<TResponse>(IRequest<TResponse> request);

  Task Send(IRequest request);
}