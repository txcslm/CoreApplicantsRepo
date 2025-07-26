using ChatSystem.Application;
using ChatSystem.Application.Interfaces;
using ChatSystem.Infrastructure;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Network.Interfaces;
using ChatSystem.Presentation;
using ChatSystem.Presentation.Services;

namespace ChatSystem.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
  [Test]
  public void AddApplicationServices_RegistersServices()
  {
    var services = new ServiceCollection();

    services.AddApplicationServices();

    var serviceProvider = services.BuildServiceProvider();
    var mediator = serviceProvider.GetService<IMediator>();

    Assert.That(mediator, Is.Not.Null);
    Assert.That(mediator, Is.TypeOf<Mediator>());
  }

  [Test]
  public void AddMockNetworkInfrastructure_RegistersAllNetworkServices()
  {
    var services = new ServiceCollection();

    services.AddMockNetworkInfrastructure("test-client", 100);

    var serviceProvider = services.BuildServiceProvider();

    Assert.Multiple(() =>
    {
      Assert.That(serviceProvider.GetService<IChatNetwork>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<IMessageSender>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<IMessageReceiver>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<IEventPublisher>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<IEventSubscriber>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<IConnectionManager>(), Is.Not.Null);
      Assert.That(serviceProvider.GetService<ITeamMember>(), Is.Not.Null);
    });
  }

  [Test]
  public void AddPresentationServices_RegistersChatManager()
  {
    var services = new ServiceCollection();
    services.AddApplicationServices();
    services.AddMockNetworkInfrastructure("test-client", 100);

    services.AddPresentationServices();

    var serviceProvider = services.BuildServiceProvider();
    var chatManager = serviceProvider.GetService<ChatManager>();

    Assert.That(chatManager, Is.Not.Null);
  }

  [Test]
  public void RegisterMultipleMockClients_CreatesIndependentInstances()
  {
    var services1 = new ServiceCollection();
    var services2 = new ServiceCollection();

    services1.AddMockNetworkInfrastructure("client1", 100);
    services2.AddMockNetworkInfrastructure("client2");

    var provider1 = services1.BuildServiceProvider();
    var provider2 = services2.BuildServiceProvider();

    var network1 = provider1.GetService<IChatNetwork>();
    var network2 = provider2.GetService<IChatNetwork>();

    Assert.Multiple(() =>
    {
      Assert.That(network1, Is.Not.Null);
      Assert.That(network2, Is.Not.Null);
      Assert.That(network1?.ClientId, Is.EqualTo("client1"));
      Assert.That(network2?.ClientId, Is.EqualTo("client2"));
    });
  }
}