using ChatSystem.Application.Interfaces;
using ChatSystem.Domain.ValueObjects;

namespace ChatSystem.Application.Commands;

public class SendMessageCommand : IRequest<bool>
{
  public string Content { get; set; } = string.Empty;

  public string Sender { get; set; } = string.Empty;

  public ChatType Type { get; set; }

  public string? TeamId { get; set; }
}