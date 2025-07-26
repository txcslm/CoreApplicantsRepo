using ChatSystem.Application.Interfaces;
using ChatSystem.Domain.Entities;

namespace ChatSystem.Application.Notifications;

public class ChatNotification : INotification
{
  public ChatMessage Message { get; set; } = new ChatMessage();

  public string TargetClientId { get; set; } = string.Empty;
}