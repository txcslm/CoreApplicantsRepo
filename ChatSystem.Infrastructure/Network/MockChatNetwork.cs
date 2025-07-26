using System.Reactive.Subjects;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Infrastructure.Network;

public class MockChatNetwork : IChatNetwork
{
  private readonly static List<MockChatNetwork> s_allClients = new List<MockChatNetwork>();
  private readonly static HashSet<string> s_processedMessageIds = new HashSet<string>();
  private readonly static Lock s_lock = new Lock();
  
  private static IConnectionStateSubject? s_connectionStateManager;
  
  private readonly Subject<(EventType, object)> _eventSubject = new Subject<(EventType, object)>();
  private readonly int _latencyMs;

  private readonly Subject<ChatMessage> _messageSubject = new Subject<ChatMessage>();
  private readonly Random _random = new Random();

  private bool _isConnected = true;

  public MockChatNetwork(string? clientId = null, int latencyMs = 200)
  {
    ClientId = clientId ?? Guid.NewGuid().ToString();
    _latencyMs = latencyMs;

    lock (s_lock)
    {
      s_allClients.Add(this);
    }
  }

  private string? TeamId { get; set; }

  public string ClientId { get; }

  public ISubject<ChatMessage> OnMessageReceived => _messageSubject;

  public ISubject<(EventType, object)> OnEventReceived => _eventSubject;

  public void SimulateDisconnect()
  {
    _isConnected = false;
    Console.WriteLine($"[MockChatNetwork] Client {ClientId} disconnected");
    s_connectionStateManager?.NotifyObservers(ClientId, ConnectionState.Disconnected);
  }

  public void SimulateReconnect()
  {
    _isConnected = true;
    Console.WriteLine($"[MockChatNetwork] Client {ClientId} reconnected");
    s_connectionStateManager?.NotifyObservers(ClientId, ConnectionState.Connected);
  }

  public async Task SendMessageAsync(string message, string sender, ChatType chatType, string? teamId = null)
  {
    if (!_isConnected)
      throw new Exception("Disconnected");

    await Task.Delay(_latencyMs + _random.Next(0, 100));

    var chatMessage = new ChatMessage
    {
      Id = Guid.NewGuid().ToString(),
      Content = message,
      Sender = sender,
      Type = chatType,
      TeamId = teamId
    };

    BroadcastMessage(chatMessage);
  }

  public void SetTeamId(string? teamId)
  {
    TeamId = teamId;
  }

  public async Task SendMessageAsync(string message, string sender)
  {
    await SendMessageAsync(message, sender, ChatType.Public);
  }

  public async Task RaiseEventAsync(EventType eventType, object data)
  {
    if (!_isConnected) throw new Exception("Disconnected");
    await Task.Delay(_latencyMs);

    lock (s_lock)
    {
      int recipients = 0;
      foreach (var client in s_allClients.Where(c => c._isConnected))
      {
        client._eventSubject.OnNext((eventType, data));
        recipients++;
      }
      Console.WriteLine($"[MockChatNetwork] Event raised: {eventType}, delivered to {recipients} clients");
    }
  }

  public static void SetConnectionStateManager(IConnectionStateSubject connectionStateManager)
  {
    s_connectionStateManager = connectionStateManager;
  }

  public static void ClearAllClients()
  {
    lock (s_lock)
    {
      s_allClients.Clear();
    }
  }

  public static IReadOnlyList<MockChatNetwork> GetAllClients()
  {
    lock (s_lock)
    {
      return s_allClients.ToList();
    }
  }

  private static bool ShouldReceiveMessage(MockChatNetwork client, ChatMessage message)
  {
    return message.Type switch
    {
      ChatType.Public => true,
      ChatType.Team => client.TeamId == message.TeamId && !string.IsNullOrEmpty(message.TeamId),
      _ => true
    };
  }

  private static void BroadcastMessage(ChatMessage message)
  {
    lock (s_lock)
    {
      if (!s_processedMessageIds.Add(message.Id))
      {
        Console.WriteLine($"[MockChatNetwork] Duplicate message filtered: {message.Id}");
        return;
      }

      Console.WriteLine($"[MockChatNetwork] Broadcasting message from {message.Sender}: {message.Content} (ID: {message.Id}, Type: {message.Type})");

      int recipients = 0;

      foreach (var client in s_allClients.Where(c => c._isConnected))
      {
        if (!ShouldReceiveMessage(client, message))
          continue;

        client._messageSubject.OnNext(message);
        recipients++;
      }

      Console.WriteLine($"[MockChatNetwork] Message delivered to {recipients} clients");
    }
  }
}