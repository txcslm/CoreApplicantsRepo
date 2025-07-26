using System.Reactive.Subjects;
using ChatSystem.Application.Interfaces;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Network.Interfaces;
using ChatSystem.Presentation.Services;
using Moq;

namespace ChatSystem.Tests;

[TestFixture]
public class ChatManagerTests
{
  [SetUp]
  public void SetUp()
  {
    _mockMessageSender = new Mock<IMessageSender>();
    _mockMessageReceiver = new Mock<IMessageReceiver>();
    _mockEventPublisher = new Mock<IEventPublisher>();
    _mockEventSubscriber = new Mock<IEventSubscriber>();
    _mockConnectionManager = new Mock<IConnectionManager>();
    _mockMediator = new Mock<IMediator>();
    _mockRetryStrategy = new Mock<IRetryStrategy>();
    _publicProcessor = new PublicMessageProcessor();
    _teamProcessor = new TeamMessageProcessor();

    _mockMessageReceiver.Setup(n => n.OnMessageReceived).Returns(new Subject<ChatMessage>());
    _mockEventSubscriber.Setup(n => n.OnEventReceived).Returns(new Subject<(EventType, object)>());

    _mockRetryStrategy.Setup(rs => rs.ExecuteAsync(It.IsAny<Func<Task>>()))
      .Returns<Func<Task>>(async func =>
      {
        try
        {
          await func();
        }
        catch (Exception)
        {
          await Task.Delay(500);
          await func();
        }
      });
  }

  private Mock<IMessageSender> _mockMessageSender;
  private Mock<IMessageReceiver> _mockMessageReceiver;
  private Mock<IEventPublisher> _mockEventPublisher;
  private Mock<IEventSubscriber> _mockEventSubscriber;
  private Mock<IConnectionManager> _mockConnectionManager;
  private Mock<IMediator> _mockMediator;
  private Mock<IRetryStrategy> _mockRetryStrategy;
  private PublicMessageProcessor _publicProcessor;
  private TeamMessageProcessor _teamProcessor;
  private ChatManager? _manager;

  [Test]
  public async Task SendMessageAsync_CallsNetworkSend_AndBroadcasts()
  {
    Subject<ChatMessage> messageSubject = new Subject<ChatMessage>();
    _mockMessageReceiver.Setup(n => n.OnMessageReceived).Returns(messageSubject);
    ChatMessage? receivedMessage = null;
    _manager = new ChatManager(_mockMessageSender.Object,
      _mockMessageReceiver.Object,
      _mockEventPublisher.Object,
      _mockEventSubscriber.Object,
      _mockConnectionManager.Object,
      _mockMediator.Object,
      _mockRetryStrategy.Object,
      _publicProcessor,
      _teamProcessor);
    _manager.Messages.Subscribe(msg => { receivedMessage = msg; });

    _mockMessageSender.Setup(n => n.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
      .Returns(Task.CompletedTask)
      .Callback<string, string>((msg, sender) =>
      {
        var chatMessage = new ChatMessage { Content = msg, Sender = sender, Type = ChatType.Public };
        messageSubject.OnNext(chatMessage);
      });

    await _manager.SendChatMessageAsync("Hello", "Player1");

    _mockMessageSender.Verify(n => n.SendMessageAsync("Hello", "Player1"), Times.Once());
    Assert.Multiple(() =>
    {
      Assert.That(receivedMessage?.Content, Is.EqualTo("Hello"));
      Assert.That(receivedMessage?.Sender, Is.EqualTo("Player1"));
    });
  }

  [Test]
  public async Task SendNotificationAsync_WithBuilder_CallsRaiseEvent()
  {
    Subject<(EventType, object)> eventSubject = new Subject<(EventType, object)>();
    _mockEventSubscriber.Setup(n => n.OnEventReceived).Returns(eventSubject);
    (EventType, object) receivedEvent = default;
    _manager = new ChatManager(_mockMessageSender.Object,
      _mockMessageReceiver.Object,
      _mockEventPublisher.Object,
      _mockEventSubscriber.Object,
      _mockConnectionManager.Object,
      _mockMediator.Object,
      _mockRetryStrategy.Object,
      _publicProcessor,
      _teamProcessor);
    _manager.Events.Subscribe(ev => receivedEvent = ev);

    _mockEventPublisher.Setup(n => n.RaiseEventAsync(It.IsAny<EventType>(), It.IsAny<object>()))
      .Returns(Task.CompletedTask)
      .Callback<EventType, object>((type, data) =>
        eventSubject.OnNext((type, data)));

    var builder = new NotificationBuilder()
      .SetType(EventType.KillNotification)
      .SetMessage("Player1 killed Player2");
    var notification = builder.BuildLegacy();

    await _manager.SendNotificationAsync(notification.Item1, notification.Item2);

    _mockEventPublisher.Verify(n => n.RaiseEventAsync(EventType.KillNotification, "Player1 killed Player2"), Times.Once());
    Assert.Multiple(() =>
    {
      Assert.That(receivedEvent.Item1, Is.EqualTo(EventType.KillNotification));
      Assert.That(receivedEvent.Item2, Is.EqualTo("Player1 killed Player2"));
    });
  }
}