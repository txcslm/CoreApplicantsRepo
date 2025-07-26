using System.Text;
using ChatSystem.Application;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Services;
using ChatSystem.Domain.ValueObjects;
using ChatSystem.Infrastructure;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Demo;

internal class Program
{
  private static async Task Main(string[] args)
  {
    var output = new StringBuilder();
    output.AppendLine("=== Chat System Demo ===");
    output.AppendLine();

    var services = new ServiceCollection();
    services.AddApplicationServices();
    services.AddMockNetworkInfrastructure("DemoClient", 100);
    services.AddPresentationServices();

    var serviceProvider = services.BuildServiceProvider();

    var networkFactory = serviceProvider.GetRequiredService<IChatNetworkFactory>();

    var client1 = (MockChatNetwork)networkFactory.CreateClient("Player1", 100, "TeamA");
    var client2 = (MockChatNetwork)networkFactory.CreateClient("Player2", 100, "TeamA");
    var client3 = (MockChatNetwork)networkFactory.CreateClient("Player3", 100, "TeamB");

    string? teamA = "TeamA";
    string? teamB = "TeamB";

    output.AppendLine("Created 3 clients:");
    output.AppendLine($"- Player1 ({teamA})");
    output.AppendLine($"- Player2 ({teamA})");
    output.AppendLine($"- Player3 ({teamB})");
    output.AppendLine();

    List<ChatMessage> allMessages = new List<ChatMessage>();

    client1.OnMessageReceived.Subscribe(msg =>
    {
      allMessages.Add(msg);
      output.AppendLine($"[{msg.Type}] {msg.Sender}: {msg.Content}");
    });

    client2.OnMessageReceived.Subscribe(msg =>
    {
      allMessages.Add(msg);
      output.AppendLine($"[{msg.Type}] {msg.Sender}: {msg.Content}");
    });

    client3.OnMessageReceived.Subscribe(msg =>
    {
      allMessages.Add(msg);
      output.AppendLine($"[{msg.Type}] {msg.Sender}: {msg.Content}");
    });

    output.AppendLine("=== Demo 1: Public Chat ===");
    await client1.SendMessageAsync("Hello everyone!", "Player1", ChatType.Public);
    await Task.Delay(200);

    output.AppendLine($"Messages received: {allMessages.Count}");
    output.AppendLine();
    allMessages.Clear();

    output.AppendLine("=== Demo 2: Team Chat ===");
    await client1.SendMessageAsync("Team A strategy", "Player1", ChatType.Team, teamA);
    await Task.Delay(200);

    output.AppendLine($"Messages received: {allMessages.Count} (should be 1 - only TeamA members)");
    output.AppendLine();
    allMessages.Clear();

    output.AppendLine("=== Demo 3: Event Notifications ===");
    List<(EventType, object)> eventReceived = new List<(EventType, object)>();

    client1.OnEventReceived.Subscribe(evt =>
    {
      eventReceived.Add(evt);
      output.AppendLine($"Event: {evt.Item1} - {evt.Item2}");
    });

    client2.OnEventReceived.Subscribe(evt =>
    {
      eventReceived.Add(evt);
      output.AppendLine($"Event: {evt.Item1} - {evt.Item2}");
    });

    client3.OnEventReceived.Subscribe(evt =>
    {
      eventReceived.Add(evt);
      output.AppendLine($"Event: {evt.Item1} - {evt.Item2}");
    });

    await client1.RaiseEventAsync(EventType.MatchStart, "Game is starting!");
    await Task.Delay(200);

    output.AppendLine($"Events received: {eventReceived.Count}");
    output.AppendLine();

    output.AppendLine("=== Demo 4: NotificationBuilder ===");
    var killNotification = NotificationBuilder.ForKill("Player1", "Player3").Build();
    output.AppendLine($"Kill notification: {killNotification.Message}");
    output.AppendLine($"Metadata: Killer={killNotification.Metadata["killer"]}, Victim={killNotification.Metadata["victim"]}");

    var matchNotification = NotificationBuilder.ForMatchStart().Build();
    output.AppendLine($"Match notification: {matchNotification.Message}");
    output.AppendLine($"Has start time: {matchNotification.Metadata.ContainsKey("startTime")}");
    output.AppendLine();

    output.AppendLine("=== Demo 5: Disconnect/Reconnect ===");
    output.AppendLine("Disconnecting Player2...");
    client2.SimulateDisconnect();

    await client1.SendMessageAsync("Player2 should not see this", "Player1", ChatType.Public);
    await Task.Delay(200);

    output.AppendLine($"Messages received while Player2 disconnected: {allMessages.Count}");

    output.AppendLine("Reconnecting Player2...");
    client2.SimulateReconnect();
    allMessages.Clear();

    await client1.SendMessageAsync("Player2 should see this", "Player1", ChatType.Public);
    await Task.Delay(200);

    output.AppendLine($"Messages received after Player2 reconnected: {allMessages.Count}");
    output.AppendLine();

    output.AppendLine("=== Demo Complete ===");
    output.AppendLine($"Total clients created: {MockChatNetwork.GetAllClients().Count}");

    Console.Write(output.ToString());

    MockChatNetwork.ClearAllClients();
    await serviceProvider.DisposeAsync();
  }
}