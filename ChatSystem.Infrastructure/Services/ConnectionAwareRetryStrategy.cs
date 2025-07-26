using ChatSystem.Domain.Services;
using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Infrastructure.Services;

public class ConnectionAwareRetryStrategy : IRetryStrategy
{
  private readonly IConnectionManager _connectionManager;
  private readonly IRetryStrategy _innerStrategy;

  public ConnectionAwareRetryStrategy(IRetryStrategy innerStrategy, IConnectionManager connectionManager)
  {
    _innerStrategy = innerStrategy;
    _connectionManager = connectionManager;
  }

  public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
  {
    return await _innerStrategy.ExecuteAsync(async () =>
    {
      try
      {
        return await operation();
      }
      catch (Exception)
      {
        _connectionManager.SimulateReconnect();
        throw;
      }
    });
  }

  public async Task ExecuteAsync(Func<Task> operation)
  {
    await _innerStrategy.ExecuteAsync(async () =>
    {
      try
      {
        await operation();
      }
      catch (Exception)
      {
        _connectionManager.SimulateReconnect();
        throw;
      }
    });
  }
}