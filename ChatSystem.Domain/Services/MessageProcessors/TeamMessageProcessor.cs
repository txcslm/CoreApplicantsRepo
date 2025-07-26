using ChatSystem.Domain.Entities;

namespace ChatSystem.Domain.Services;

public class TeamMessageProcessor : MessageProcessor
{
  protected override async Task ProcessMessageCoreAsync(ChatMessage message)
  {
    await Task.CompletedTask;
    Console.WriteLine($"[TeamMessageProcessor] Broadcasting team message to team {message.TeamId}: {message.Content}");
  }

  protected override async Task<bool> ValidateMessageAsync(ChatMessage message)
  {
    bool baseValidation = await base.ValidateMessageAsync(message);
    return baseValidation && !string.IsNullOrWhiteSpace(message.TeamId);
  }

  protected override async Task OnValidationFailedAsync(ChatMessage message)
  {
    await base.OnValidationFailedAsync(message);
    Console.WriteLine("[TeamMessageProcessor] Team message requires valid TeamId");
  }
}