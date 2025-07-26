namespace ChatSystem.Domain.Services;

public interface IRetryStrategy
{
  Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

  Task ExecuteAsync(Func<Task> operation);
}