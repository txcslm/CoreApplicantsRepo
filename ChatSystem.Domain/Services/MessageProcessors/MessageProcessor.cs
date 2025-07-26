using ChatSystem.Domain.Entities;

namespace ChatSystem.Domain.Services;

public abstract class MessageProcessor
{
  public async Task<bool> ProcessMessageAsync(ChatMessage message)
  {
    try
    {
      if (!await ValidateMessageAsync(message))
      {
        await OnValidationFailedAsync(message);
        return false;
      }

      await PreProcessMessageAsync(message);

      await ProcessMessageCoreAsync(message);

      await PostProcessMessageAsync(message);

      await OnSuccessAsync(message);
      return true;
    }
    catch (Exception ex)
    {
      await OnErrorAsync(message, ex);
      return false;
    }
  }

  protected abstract Task ProcessMessageCoreAsync(ChatMessage message);

  protected virtual async Task<bool> ValidateMessageAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    return !string.IsNullOrWhiteSpace(message.Content) &&
           !string.IsNullOrWhiteSpace(message.Sender);
  }

  protected virtual async Task PreProcessMessageAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[MessageProcessor] Pre-processing message from {message.Sender}");
  }

  protected virtual async Task PostProcessMessageAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[MessageProcessor] Post-processing message from {message.Sender}");
  }

  protected virtual async Task OnValidationFailedAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[MessageProcessor] Validation failed for message from {message.Sender}");
  }

  protected virtual async Task OnSuccessAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[MessageProcessor] Successfully processed message from {message.Sender}");
  }

  protected virtual async Task OnErrorAsync(ChatMessage message, Exception ex)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[MessageProcessor] Error processing message from {message.Sender}: {ex.Message}");
  }
}