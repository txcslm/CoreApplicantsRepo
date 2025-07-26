using System.Diagnostics;
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
public class ChatManagerRetryTests
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
          _mockConnectionManager.Object.SimulateReconnect();
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
  public async Task SendChatMessageAsync_WhenSenderFails_RetriesAfterReconnect()
  {
    _mockSender.SetupSequence(s => s.SendMessageAsync("Hello", "TestUser"))
      .ThrowsAsync(new Exception("Network error"))
      .Returns(Task.CompletedTask);

    await _chatManager.SendChatMessageAsync("Hello", "TestUser");

    _mockConnectionManager.Verify(c => c.SimulateReconnect(), Times.Once);
    _mockSender.Verify(s => s.SendMessageAsync("Hello", "TestUser"), Times.Exactly(2));
  }

  [Test]
  public Task SendChatMessageAsync_WhenRetrySucceeds_DoesNotThrow()
  {
    _mockSender.SetupSequence(s => s.SendMessageAsync("Test", "User"))
      .ThrowsAsync(new Exception("Temporary failure"))
      .Returns(Task.CompletedTask);

    Assert.DoesNotThrowAsync(async () =>
      await _chatManager.SendChatMessageAsync("Test", "User"));

    return Task.CompletedTask;
  }

  [Test]
  public Task SendChatMessageAsync_WhenBothCallsFail_ThrowsException()
  {
    _mockSender.Setup(s => s.SendMessageAsync("Fail", "User"))
      .ThrowsAsync(new Exception("Persistent error"));

    var ex = Assert.ThrowsAsync<Exception>(async () =>
      await _chatManager.SendChatMessageAsync("Fail", "User"));

    Assert.That(ex.Message, Is.EqualTo("Persistent error"));
    _mockConnectionManager.Verify(c => c.SimulateReconnect(), Times.Once);
    _mockSender.Verify(s => s.SendMessageAsync("Fail", "User"), Times.Exactly(2));

    return Task.CompletedTask;
  }

  [Test]
  public async Task SendChatMessageAsync_IncludesProperDelayBetweenRetries()
  {
    _mockRetryStrategy.Setup(rs => rs.ExecuteAsync(It.IsAny<Func<Task>>()))
      .Returns<Func<Task>>(async func =>
      {
        try
        {
          await func();
        }
        catch (Exception)
        {
          _mockConnectionManager.Object.SimulateReconnect();
          await Task.Delay(500);
          await func();
        }
      });

    var stopwatch = Stopwatch.StartNew();
    _mockSender.SetupSequence(s => s.SendMessageAsync("Delay", "User"))
      .ThrowsAsync(new Exception("First fail"))
      .Returns(Task.CompletedTask);

    await _chatManager.SendChatMessageAsync("Delay", "User");
    stopwatch.Stop();

    Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThan(450));
    _mockConnectionManager.Verify(c => c.SimulateReconnect(), Times.Once);
  }

  [Test]
  public async Task SendNotificationAsync_CallsEventPublisherDirectly()
  {
    var eventType = EventType.KillNotification;
    string data = "test data";

    await _chatManager.SendNotificationAsync(eventType, data);
    _mockEventPublisher.Verify(e => e.RaiseEventAsync(eventType, data), Times.Once);
  }

  [Test]
  public Task SendNotificationAsync_WhenEventPublisherFails_DoesNotRetry()
  {
    _mockEventPublisher.Setup(e => e.RaiseEventAsync(It.IsAny<EventType>(), It.IsAny<object>()))
      .ThrowsAsync(new Exception("Event failed"));

    var ex = Assert.ThrowsAsync<Exception>(async () =>
      await _chatManager.SendNotificationAsync(EventType.MatchStart, "data"));

    Assert.That(ex.Message, Is.EqualTo("Event failed"));
    _mockConnectionManager.Verify(c => c.SimulateReconnect(), Times.Never);

    return Task.CompletedTask;
  }

  [Test]
  public Task SendTeamMessageAsync_WithMediatorFailure_PropagatesException()
  {
    _mockMediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>()))
      .ThrowsAsync(new InvalidOperationException("Mediator error"));

    var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await _chatManager.SendTeamMessageAsync("Team msg", "Player", ChatType.Team, "TeamA"));

    Assert.That(ex.Message, Is.EqualTo("Mediator error"));

    return Task.CompletedTask;
  }
}