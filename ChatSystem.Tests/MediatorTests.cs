using ChatSystem.Application;
using ChatSystem.Application.Interfaces;

namespace ChatSystem.Tests;

[TestFixture]
public class MediatorTests
{
  [SetUp]
  public void SetUp()
  {
    _mediator = new Mediator();
  }

  private Mediator _mediator = null!;

  [Test]
  public async Task Send_WithRegisteredHandler_CallsHandler()
  {
    var handler = new TestRequestHandler();
    _mediator.RegisterHandler(handler);
    var request = new TestRequest { Message = "Test" };

    bool result = await _mediator.Send(request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(handler.HandledRequest?.Message, Is.EqualTo("Test"));
        });
    }

  [Test]
  public void Send_WithUnregisteredHandler_ThrowsException()
  {
    var request = new TestRequest { Message = "Test" };

    Assert.ThrowsAsync<InvalidOperationException>(async () => await _mediator.Send(request));
  }

  [Test]
  public async Task Publish_WithSubscribedHandlers_CallsAllHandlers()
  {
    var handler1 = new TestNotificationHandler();
    var handler2 = new TestNotificationHandler();

    _mediator.Subscribe(handler1);
    _mediator.Subscribe(handler2);

    var notification = new TestNotification { Message = "Test Notification" };

    await _mediator.Publish(notification);

    Assert.Multiple(() =>
    {
      Assert.That(handler1.HandledNotification?.Message, Is.EqualTo("Test Notification"));
      Assert.That(handler2.HandledNotification?.Message, Is.EqualTo("Test Notification"));
    });
  }

  [Test]
  public Task Publish_WithNoSubscribers_DoesNotThrow()
  {
    var notification = new TestNotification { Message = "Test" };

    Assert.DoesNotThrowAsync(async () => await _mediator.Publish(notification));

    return Task.CompletedTask;
  }
}

public class TestRequest : IRequest<bool>
{
  public string Message { get; init; } = string.Empty;
}

public class TestRequestHandler : IRequestHandler<TestRequest, bool>
{
  public TestRequest? HandledRequest { get; private set; }

  public Task<bool> Handle(TestRequest request)
  {
    HandledRequest = request;
    return Task.FromResult(true);
  }
}

public class TestNotification : INotification
{
  public string Message { get; init; } = string.Empty;
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
  public TestNotification? HandledNotification { get; private set; }

  public Task Handle(TestNotification notification)
  {
    HandledNotification = notification;
    return Task.CompletedTask;
  }
}