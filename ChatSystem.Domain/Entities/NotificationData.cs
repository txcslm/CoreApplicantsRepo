using ChatSystem.Domain.Entities;

namespace ChatSystem.Domain.Services;

public class NotificationData
{
  public EventType Type { get; set; }

  public string Message { get; set; } = string.Empty;

  public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}