using ChatSystem.Domain.Services;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Network.Interfaces;
using ChatSystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Infrastructure;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMockNetworkInfrastructure(this IServiceCollection services,
    string? clientId = null,
    int latencyMs = 200)
  {
    services.AddSingleton<IConnectionStateSubject, ConnectionStateManager>();
    services.AddTransient<IConnectionStateObserver, ConnectionLogger>();

    services.AddSingleton<MockChatNetwork>(provider =>
    {
      var network = new MockChatNetwork(clientId, latencyMs);
      var connectionStateManager = provider.GetRequiredService<IConnectionStateSubject>();
      var connectionObserver = provider.GetRequiredService<IConnectionStateObserver>();

      MockChatNetwork.SetConnectionStateManager(connectionStateManager);
      connectionStateManager.Subscribe(connectionObserver);

      return network;
    });

    services.AddSingleton<IChatNetworkFactory, MockChatNetworkFactory>();
    services.AddTransient<IRetryStrategy>(provider =>
    {
      var baseStrategy = new ExponentialBackoffRetryStrategy(3, TimeSpan.FromMilliseconds(500));
      var connectionManager = provider.GetRequiredService<IConnectionManager>();
      return new ConnectionAwareRetryStrategy(baseStrategy, connectionManager);
    });

    services.AddTransient<PublicMessageProcessor>();
    services.AddTransient<TeamMessageProcessor>();

    services.AddSingleton<IChatNetwork>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<IMessageSender>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<IMessageReceiver>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<IEventPublisher>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<IEventSubscriber>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<IConnectionManager>(provider =>
      provider.GetRequiredService<MockChatNetwork>());
    services.AddSingleton<ITeamMember>(provider =>
      provider.GetRequiredService<MockChatNetwork>());

    return services;
  }
}