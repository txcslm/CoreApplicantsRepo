using System.Reactive.Subjects;
using ChatSystem.Application.Commands;
using ChatSystem.Application.Interfaces;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Network.Interfaces;

namespace ChatSystem.Presentation.Services;

public class ChatManager
{
  private readonly IConnectionManager _connectionManager;
  private readonly IEventPublisher _eventPublisher;
  private readonly Subject<(EventType, object)> _events = new Subject<(EventType, object)>();
  private readonly IMediator _mediator;
  private readonly Subject<ChatMessage> _messages = new Subject<ChatMessage>();
  private readonly IMessageSender _messageSender;
  private readonly PublicMessageProcessor _publicMessageProcessor;
  private readonly IRetryStrategy _retryStrategy;
  private readonly TeamMessageProcessor _teamMessageProcessor;

  public ChatManager(IMessageSender messageSender,
    IMessageReceiver messageReceiver,
    IEventPublisher eventPublisher,
    IEventSubscriber eventSubscriber,
    IConnectionManager connectionManager,
    IMediator mediator,
    IRetryStrategy retryStrategy,
    PublicMessageProcessor publicMessageProcessor,
    TeamMessageProcessor teamMessageProcessor)
  {
    _messageSender = messageSender;
    _eventPublisher = eventPublisher;
    _connectionManager = connectionManager;
    _mediator = mediator;
    _retryStrategy = retryStrategy;
    _publicMessageProcessor = publicMessageProcessor;
    _teamMessageProcessor = teamMessageProcessor;

    messageReceiver.OnMessageReceived.Subscribe(OnMessageReceived);
    eventSubscriber.OnEventReceived.Subscribe(_events.OnNext);
  }

  public IObservable<ChatMessage> Messages => _messages;

  public IObservable<(EventType, object)> Events => _events;

  public async Task<bool> SendTeamMessageAsync(string message, string sender, ChatType chatType, string? teamId = null)
  {
    var command = new SendMessageCommand
    {
      Content = message,
      Sender = sender,
      Type = chatType,
      TeamId = teamId
    };

    return await _mediator.Send(command);
  }

  private async void OnMessageReceived(ChatMessage message)
  {
    MessageProcessor processor = message.Type == ChatType.Team
      ? _teamMessageProcessor
      : _publicMessageProcessor;

    bool processed = await processor.ProcessMessageAsync(message);

    if (processed)
    {
      _messages.OnNext(message);
    }
  }

  public async Task SendChatMessageAsync(string message, string sender)
  {
    await _retryStrategy.ExecuteAsync(async () => { await _messageSender.SendMessageAsync(message, sender); });
  }

  public async Task SendNotificationAsync(EventType eventType, object data)
  {
    await _eventPublisher.RaiseEventAsync(eventType, data);
  }
}