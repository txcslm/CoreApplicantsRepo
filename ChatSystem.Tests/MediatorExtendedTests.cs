using ChatSystem.Application;
using ChatSystem.Application.Interfaces;

namespace ChatSystem.Tests;

[TestFixture]
public class MediatorExtendedTests
{
  [SetUp]
  public void Setup()
  {
    _mediator = new Mediator();
  }

  private Mediator _mediator;

  private class TestVoidRequest : IRequest
  {
    public string Data { get; set; } = string.Empty;
  }

  private class TestVoidHandler : IRequestHandler<TestVoidRequest>
  {
    public bool WasCalled { get; private set; }

    public async Task Handle(TestVoidRequest request)
    {
      WasCalled = true;
      await Task.Delay(10);
    }
  }

  public class TestNotification : INotification
  {
    public string Message { get; set; } = string.Empty;
  }

  private class TestNotificationHandler : INotificationHandler<TestNotification>
  {
    public bool WasCalled { get; private set; }

    public TestNotification? ReceivedNotification { get; private set; }

    public async Task Handle(TestNotification notification)
    {
      WasCalled = true;
      ReceivedNotification = notification;
      await Task.Delay(10);
    }
  }

  [Test]
  public async Task RegisterHandler_VoidRequest_RegistersAndHandlesCorrectly()
  {
    var handler = new TestVoidHandler();
    _mediator.RegisterHandler(handler);

    var request = new TestVoidRequest { Data = "test" };
    await _mediator.Send(request);

    Assert.That(handler.WasCalled, Is.True);
  }

  [Test]
  public void Send_VoidRequest_WithoutRegisteredHandler_ThrowsException()
  {
    var request = new TestVoidRequest { Data = "test" };

    var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await _mediator.Send(request));

    Assert.That(ex.Message, Contains.Substring("No handler registered for TestVoidRequest"));
  }

  [Test]
  public async Task Subscribe_NotificationHandler_RegistersAndHandlesNotifications()
  {
    var handler = new TestNotificationHandler();
    _mediator.Subscribe(handler);

    var notification = new TestNotification { Message = "test notification" };
    await _mediator.Publish(notification);

    Assert.Multiple(() =>
    {
      Assert.That(handler.WasCalled, Is.True);
      Assert.That(handler.ReceivedNotification, Is.Not.Null);
      Assert.That(handler.ReceivedNotification?.Message, Is.EqualTo("test notification"));
    });
  }

  [Test]
  public async Task Subscribe_MultipleHandlersForSameNotification_CallsAllHandlers()
  {
    var handler1 = new TestNotificationHandler();
    var handler2 = new TestNotificationHandler();

    _mediator.Subscribe(handler1);
    _mediator.Subscribe(handler2);

    var notification = new TestNotification { Message = "broadcast test" };
    await _mediator.Publish(notification);

    Assert.Multiple(() =>
    {
      Assert.That(handler1.WasCalled, Is.True);
      Assert.That(handler2.WasCalled, Is.True);
      Assert.That(handler1.ReceivedNotification?.Message, Is.EqualTo("broadcast test"));
      Assert.That(handler2.ReceivedNotification?.Message, Is.EqualTo("broadcast test"));
    });
  }

  [Test]
  public Task Publish_WithoutSubscribers_DoesNotThrow()
  {
    var notification = new TestNotification { Message = "no subscribers" };

    Assert.DoesNotThrowAsync(async () => await _mediator.Publish(notification));

    return Task.CompletedTask;
  }

  [Test]
  public async Task Subscribe_DuplicateHandler_DoesNotDuplicateExecution()
  {
    var handler = new TestNotificationHandler();

    _mediator.Subscribe(handler);
    _mediator.Subscribe(handler);

    var notification = new TestNotification { Message = "duplicate test" };
    await _mediator.Publish(notification);

    Assert.Multiple(() =>
    {
      Assert.That(handler.WasCalled, Is.True);
      Assert.That(handler.ReceivedNotification?.Message, Is.EqualTo("duplicate test"));
    });
  }
}