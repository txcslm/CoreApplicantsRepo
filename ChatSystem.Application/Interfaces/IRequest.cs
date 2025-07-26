namespace ChatSystem.Application.Interfaces;

public interface IRequest
{
}

public interface IRequest<out TResponse> : IRequest
{
}