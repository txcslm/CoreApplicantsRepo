using ChatSystem.Domain.Services;

namespace ChatSystem.Tests;

[TestFixture]
public class RetryStrategyTests
{
  [SetUp]
  public void Setup()
  {
    _retryStrategy = new ExponentialBackoffRetryStrategy();
  }

  private ExponentialBackoffRetryStrategy _retryStrategy = null!;

  [Test]
  public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResult()
  {
    string result = await _retryStrategy.ExecuteAsync(async () =>
    {
      await Task.Delay(10);
      return "Success";
    });

    Assert.That(result, Is.EqualTo("Success"));
  }

  [Test]
  public Task ExecuteAsync_WithFailingOperation_RetriesAndThrows()
  {
    int attempts = 0;

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
      await _retryStrategy.ExecuteAsync(async () =>
      {
        attempts++;
        await Task.Delay(10);
        throw new InvalidOperationException("Test failure");
      });
    });

    Assert.That(attempts, Is.GreaterThan(1), "Should retry multiple times");
    
    return Task.CompletedTask;
  }

  [Test]
  public async Task ExecuteAsync_WithEventualSuccess_ReturnsResult()
  {
    int attempts = 0;

    string result = await _retryStrategy.ExecuteAsync(async () =>
    {
      attempts++;
      await Task.Delay(10);

      if (attempts < 3)
        throw new InvalidOperationException("Temporary failure");

      return "Success after retries";
    });

    Assert.That(result, Is.EqualTo("Success after retries"));
    Assert.That(attempts, Is.EqualTo(3));
  }

  [Test]
  public async Task ExecuteAsync_VoidOperation_WithSuccess_Completes()
  {
    bool executed = false;

    await _retryStrategy.ExecuteAsync(async () =>
    {
      await Task.Delay(10);
      executed = true;
    });

    Assert.That(executed, Is.True);
  }

  [Test]
  public Task ExecuteAsync_VoidOperation_WithFailure_RetriesAndThrows()
  {
    int attempts = 0;

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
      await _retryStrategy.ExecuteAsync(async () =>
      {
        attempts++;
        await Task.Delay(10);
        throw new InvalidOperationException("Test failure");
      });
    });

    Assert.That(attempts, Is.GreaterThan(1));
    
    return Task.CompletedTask;
  }
}