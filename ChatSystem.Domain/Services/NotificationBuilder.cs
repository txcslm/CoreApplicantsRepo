using ChatSystem.Domain.Entities;

namespace ChatSystem.Domain.Services;

public class NotificationBuilder
{
  private readonly Dictionary<string, object> _metadata = new Dictionary<string, object>();
  private string _message = string.Empty;
  private EventType _type;

  public NotificationBuilder SetType(EventType type)
  {
    _type = type;
    return this;
  }

  public NotificationBuilder SetMessage(string message)
  {
    _message = message;
    return this;
  }

  public NotificationBuilder AddMetadata(string key, object value)
  {
    _metadata[key] = value;
    return this;
  }

  public NotificationData Build() =>
    new NotificationData
    {
      Type = _type,
      Message = _message,
      Metadata = new Dictionary<string, object>(_metadata)
    };

  public (EventType, object) BuildLegacy() =>
    (_type, _message);

  public static NotificationBuilder ForKill(string killer, string victim) =>
    new NotificationBuilder()
      .SetType(EventType.KillNotification)
      .SetMessage($"{killer} killed {victim}")
      .AddMetadata("killer", killer)
      .AddMetadata("victim", victim);

  public static NotificationBuilder ForMatchStart() =>
    new NotificationBuilder()
      .SetType(EventType.MatchStart)
      .SetMessage("Match is starting!");
}