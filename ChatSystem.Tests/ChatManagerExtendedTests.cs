using System.Reactive.Subjects;
using ChatSystem.Application.Commands;
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
public class ChatManagerExtendedTests
{
  [SetUp]
  public void Setup()
  {
    _mockSender = new Mock<IMessageSender>();
    _mockReceiver = new Mock<IMessageReceiver>();
    _mockEventPublisher = new Mock<IEventPublisher>();
    _mockEventSubscriber = new Mock<IEventSubscriber>();
    _mockConnectionManager = new Mock<IConnectionManager>();
    _mockMediator = new Mock<IMediator>();
    _mockRetryStrategy = new Mock<IRetryStrategy>();
    _publicProcessor = new PublicMessageProcessor();
    _teamProcessor = new TeamMessageProcessor();

    _messageSubject = new Subject<ChatMessage>();
    _eventSubject = new Subject<(EventType, object)>();

    _mockReceiver.Setup(r => r.OnMessageReceived).Returns(_messageSubject);
    _mockEventSubscriber.Setup(r => r.OnEventReceived).Returns(_eventSubject);

    _mockRetryStrategy.Setup(rs => rs.ExecuteAsync(It.IsAny<Func<Task>>()))
      .Returns<Func<Task>>(async func =>
      {
        try
        {
          await func();
        }
        catch (Exception)
        {
          await Task.Delay(100);
          await func();
        }
      });

    _chatManager = new ChatManager(_mockSender.Object,
      _mockReceiver.Object,
      _mockEventPublisher.Object,
      _mockEventSubscriber.Object,
      _mockConnectionManager.Object,
      _mockMediator.Object,
      _mockRetryStrategy.Object,
      _publicProcessor,
      _teamProcessor);
  }

  [TearDown]
  public void TearDown()
  {
    _messageSubject?.Dispose();
    _eventSubject?.Dispose();
  }

  private Mock<IMessageSender> _mockSender;
  private Mock<IMessageReceiver> _mockReceiver;
  private Mock<IEventPublisher> _mockEventPublisher;
  private Mock<IEventSubscriber> _mockEventSubscriber;
  private Mock<IConnectionManager> _mockConnectionManager;
  private Mock<IMediator> _mockMediator;
  private Mock<IRetryStrategy> _mockRetryStrategy;
  private PublicMessageProcessor _publicProcessor;
  private TeamMessageProcessor _teamProcessor;
  private Subject<ChatMessage> _messageSubject;
  private Subject<(EventType, object)> _eventSubject;
  private ChatManager _chatManager;

  [Test]
  public async Task SendChatMessageAsync_CallsNetworkSender()
  {
    await _chatManager.SendChatMessageAsync("Hello", "TestUser");

    _mockSender.Verify(s => s.SendMessageAsync("Hello", "TestUser"), Times.Once);
  }

  [Test]
  public async Task SendTeamMessageAsync_CallsMediatorWithCommand()
  {
    _mockMediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>())).ReturnsAsync(true);

    bool result = await _chatManager.SendTeamMessageAsync("Team msg", "Player", ChatType.Team, "TeamA");

    _mockMediator.Verify(m => m.Send(It.Is<SendMessageCommand>(cmd =>
        cmd.Content == "Team msg" &&
        cmd.Sender == "Player" &&
        cmd.Type == ChatType.Team &&
        cmd.TeamId == "TeamA")),
      Times.Once);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task SendNotificationAsync_CallsNetworkEventPublisher()
  {
    await _chatManager.SendNotificationAsync(EventType.MatchStart, "Game starting");

    _mockEventPublisher.Verify(e => e.RaiseEventAsync(EventType.MatchStart, "Game starting"), Times.Once);
  }

  [Test]
  public void MessageReceived_AddsToMessagesList()
  {
    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    _chatManager.Messages.Subscribe(msg => receivedMessages.Add(msg));

    var message = new ChatMessage
    {
      Content = "Test message",
      Sender = "TestUser",
      Type = ChatType.Public
    };

    _messageSubject.OnNext(message);

    Assert.That(receivedMessages, Has.Count.EqualTo(1));
    Assert.That(receivedMessages[0], Is.EqualTo(message));
  }

  [Test]
  public void EventReceived_AddsToEventsList()
  {
    List<(EventType, object)> receivedEvents = new List<(EventType, object)>();
    _chatManager.Events.Subscribe(evt => receivedEvents.Add(evt));

    var eventData = (EventType.MatchStart, (object)"Match starting");

    _eventSubject.OnNext(eventData);

    Assert.That(receivedEvents, Has.Count.EqualTo(1));
    Assert.That(receivedEvents[0], Is.EqualTo(eventData));
  }

  [Test]
  public void MultipleMessages_AllStoredInOrder()
  {
    List<ChatMessage> receivedMessages = new List<ChatMessage>();
    _chatManager.Messages.Subscribe(msg => receivedMessages.Add(msg));

    var message1 = new ChatMessage { Content = "First", Sender = "User1", Type = ChatType.Public };
    var message2 = new ChatMessage { Content = "Second", Sender = "User2", Type = ChatType.Public };

    _messageSubject.OnNext(message1);
    _messageSubject.OnNext(message2);

    Assert.Multiple(() =>
    {
      Assert.That(receivedMessages, Has.Count.EqualTo(2));
      Assert.That(receivedMessages[0], Is.EqualTo(message1));
      Assert.That(receivedMessages[1], Is.EqualTo(message2));
    });
  }

  [Test]
  public void MultipleEvents_AllStoredInOrder()
  {
    List<(EventType, object)> receivedEvents = new List<(EventType, object)>();
    _chatManager.Events.Subscribe(evt => receivedEvents.Add(evt));

    var event1 = (EventType.MatchStart, (object)"Starting");
    var event2 = (EventType.KillNotification, (object)"Kill");

    _eventSubject.OnNext(event1);
    _eventSubject.OnNext(event2);

    Assert.Multiple(() =>
    {
      Assert.That(receivedEvents, Has.Count.EqualTo(2));
      Assert.That(receivedEvents[0], Is.EqualTo(event1));
      Assert.That(receivedEvents[1], Is.EqualTo(event2));
    });
  }
}