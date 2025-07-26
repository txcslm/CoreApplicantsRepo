using ChatSystem.Domain.ValueObjects;

namespace ChatSystem.Domain.Entities;

public class ChatMessage
{
  public string Id { get; init; } = Guid.NewGuid().ToString();

  public string Content { get; set; } = string.Empty;

  public string Sender { get; init; } = string.Empty;

  public ChatType Type { get; init; }

  public string? TeamId { get; init; }
}