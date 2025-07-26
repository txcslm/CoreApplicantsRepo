namespace ChatSystem.Domain.Services;

public class ExponentialBackoffRetryStrategy : IRetryStrategy
{
  private readonly TimeSpan _baseDelay;
  private readonly int _maxRetries;

  public ExponentialBackoffRetryStrategy(int maxRetries = 3, TimeSpan? baseDelay = null)
  {
    _maxRetries = maxRetries;
    _baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(500);
  }

  public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
  {
    int retryCount = 0;

    while (true)
    {
      try
      {
        return await operation();
      }
      catch (Exception) when (retryCount < _maxRetries)
      {
        retryCount++;
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
        await Task.Delay(delay);
      }
    }
  }

  public async Task ExecuteAsync(Func<Task> operation)
  {
    await ExecuteAsync(async () =>
    {
      await operation();

      return true;
    });
  }
}