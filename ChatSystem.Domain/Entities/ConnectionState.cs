namespace ChatSystem.Domain.Services;

public enum ConnectionState
{
  Connected,
  Disconnected,
  Reconnecting,
  Failed
}