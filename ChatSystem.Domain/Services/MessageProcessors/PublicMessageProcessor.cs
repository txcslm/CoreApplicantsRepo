using ChatSystem.Domain.Entities;

namespace ChatSystem.Domain.Services;

public class PublicMessageProcessor : MessageProcessor
{
  protected override async Task ProcessMessageCoreAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[PublicMessageProcessor] Broadcasting public message: {message.Content}");
  }

  protected override async Task PreProcessMessageAsync(ChatMessage message)
  {
    await base.PreProcessMessageAsync(message);
    message.Content = message.Content.Trim();
  }
}