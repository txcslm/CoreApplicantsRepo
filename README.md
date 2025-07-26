# Решение тестового задания: Система чата и уведомлений

## Обзор решения

Реализована комплексная система чата и уведомлений для мультиплеерной MOBA-сессии с использованием современной **Onion Architecture** и принципов SOLID. Система поддерживает публичный и командный чат,
уведомления о событиях игры, authority логику для дубликатов, имитацию сетевых задержек и сценарии disconnect/reconnect.

## Архитектура решения (Onion/Layered Architecture)

### Структура проекта:

```
ChatSystem/
├── ChatSystem.Domain/          # Core Domain Layer
│   ├── Entities/              # Domain entities (ChatMessage)
│   ├── ValueObjects/          # Value objects (ChatType, EventType)
│   └── Services/              # Domain services (NotificationBuilder)
├── ChatSystem.Application/     # Application Layer
│   ├── Commands/              # CQRS Commands (SendMessageCommand)
│   ├── Handlers/              # Command Handlers
│   ├── Interfaces/            # Application interfaces (IMediator)
│   ├── Notifications/         # Event notifications
│   └── Mediator.cs            # Mediator implementation
├── ChatSystem.Infrastructure/  # Infrastructure Layer
│   └── Network/               # Network implementations (MockChatNetwork)
├── ChatSystem.Presentation/    # Presentation Layer
│   └── Services/              # UI services (ChatManager)
├── ChatSystem.Tests/          # Unit Tests
└── ChatSystem.Demo/           # Console Demo
```

### Диаграмма Onion Architecture:

```
        ┌─────────────────────────────────┐
        │        Presentation             │
        │    ┌─────────────────────┐      │
        │    │   Infrastructure    │      │
        │    │ ┌─────────────────┐ │      │
        │    │ │   Application   │ │      │
        │    │ │  ┌───────────┐  │ │      │
        │    │ │  │  Domain   │  │ │      │
        │    │ │  │ (Entities)│  │ │      │
        │    │ │  └───────────┘  │ │      │
        │    │ │   (Mediator)    │ │      │
        │    │ └─────────────────┘ │      │
        │    │  (MockChatNetwork)  │      │
        │    └─────────────────────┘      │
        │      (ChatManager)              │
        └─────────────────────────────────┘

Dependencies: Presentation → Infrastructure → Application → Domain
```

## Реализованные возможности

### ✅ Архитектурные паттерны:

- **Onion Architecture**: Четкое разделение на слои Domain → Application → Infrastructure → Presentation
- **Mediator Pattern**: Полная реализация с reflection-based handler dispatch
- **Builder Pattern**: Расширенный NotificationBuilder с метаданными и static factory методами
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **CQRS**: Команды и обработчики в Application слое
- **ISP (Interface Segregation)**: Разделение IChatNetwork на специализированные интерфейсы

### ✅ Реактивное программирование:

- **System.Reactive**: IObservable/ISubject для потоков событий
- **Асинхронность**: async/await везде где возможно
- **Real-time updates**: Broadcast через Subject для мгновенных обновлений UI

### ✅ Мультиплеерная логика:

- **Broadcast**: Сообщения доставляются всем подключенным клиентам
- **Team Filtering**: Командные сообщения только участникам команды
- **Authority Logic**: Серверная фильтрация дубликатов через HashSet<string>
- **Multi-client Support**: Список клиентов с thread-safe операциями
- **Console Logging**: Подробное логирование всех сетевых операций

### ✅ Сетевая симуляция:

- **Latency**: Task.Delay(200-300ms) с randomization
- **Disconnect/Reconnect**: Полная поддержка с логированием состояний
- **Retry Logic**: Автоматические повторы в ChatManager при сбоях
- **Connection Management**: Централизованное управление состоянием подключений

### ✅ Типы чата и событий:

- **Public Chat**: Доступны всем игрокам
- **Team Chat**: Только участникам команды (фильтрация по TeamId)
- **Event Notifications**: Kill notifications, Match start с метаданными

## Имитация Photon через MockChatNetwork

### Как mocks имитируют Photon SDK:

**1. Photon Room Concept:**
```csharp
// Статический список всех подключенных клиентов (имитация комнаты)
private readonly static List<MockChatNetwork> s_allClients = new List<MockChatNetwork>();
private readonly static HashSet<string> s_processedMessageIds = new HashSet<string>();
private readonly static Lock s_lock = new Lock();

// Каждый MockChatNetwork представляет одного игрока в комнате
public MockChatNetwork(string? clientId = null, int latencyMs = 200)
{
    ClientId = clientId ?? Guid.NewGuid().ToString();
    _latencyMs = latencyMs;
    
    lock (s_lock)
    {
        s_allClients.Add(this);
    }
}
```

**2. Photon SendMessage → MockChatNetwork.SendMessageAsync:**
```csharp
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
```

**3. Photon RaiseEvent → MockChatNetwork.RaiseEventAsync:**
```csharp
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
```

**4. Photon Authority & Master Client:**
```csharp
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
```

**5. Photon Team Filtering (Custom Properties):**
```csharp
private static void DeliverToRelevantClients(ChatMessage message)
{
    var relevantClients = message.ChatType switch
    {
        ChatType.Public => s_connectedClients.ToList(), // Всем
        ChatType.Team => s_connectedClients
            .Where(client => GetClientTeam(client) == message.TeamId) // Только команде
            .ToList(),
        _ => new List<string>()
    };
    
    // Broadcast через Subject (как Photon OnEvent)
    foreach (var client in relevantClients)
    {
        _messageSubject.OnNext(message);
    }
}
```

**6. Photon Network Latency & Jitter:**
```csharp
// Имитация реальных сетевых условий
private readonly int _latencyMs; // Base latency
await Task.Delay(_latencyMs + Random.Shared.Next(-50, 50)); // Jitter ±50ms
```

**7. Photon Connection Management:**
```csharp
public async Task ConnectAsync()
{
    await Task.Delay(100); // Connection handshake
    
    lock (s_lock)
    {
        if (!s_connectedClients.Contains(ClientId))
        {
            s_connectedClients.Add(ClientId);
            Console.WriteLine($"[MockChatNetwork] Client {ClientId} connected");
            NotifyConnectionState(ConnectionState.Connected);
        }
    }
}

public async Task DisconnectAsync()
{
    await Task.Delay(50);
    
    lock (s_lock)
    {
        s_connectedClients.Remove(ClientId);
        Console.WriteLine($"[MockChatNetwork] Client {ClientId} disconnected");
        NotifyConnectionState(ConnectionState.Disconnected);
    }
}
```

**Соответствие Photon API:**

| Photon SDK                             | MockChatNetwork                 | Описание           |
|----------------------------------------|---------------------------------|--------------------|
| `PhotonNetwork.SendNext()`             | `SendMessageAsync()`            | Отправка данных    |
| `PhotonNetwork.RaiseEvent()`           | `RaiseEventAsync()`             | Custom события     |
| `OnEvent()` callback                   | `OnMessageReceived.Subscribe()` | Получение событий  |
| `PhotonNetwork.ConnectUsingSettings()` | `ConnectAsync()`                | Подключение        |
| `PhotonNetwork.Disconnect()`           | `DisconnectAsync()`             | Отключение         |
| Master Client authority                | Static HashSet filtering        | Серверная логика   |
| Room Custom Properties                 | Team filtering                  | Метаданные комнаты |
| Network lag simulation                 | `Task.Delay()` с jitter         | Сетевые задержки   |

## Ключевые технические решения

### 1. Authority Logic для дубликатов

```csharp
private static readonly HashSet<string> s_processedMessageIds = new();

private static void BroadcastMessage(ChatMessage message)
{
    lock (s_lock)
    {
        if (s_processedMessageIds.Contains(message.Id))
        {
            Console.WriteLine($"[MockChatNetwork] Duplicate message filtered: {message.Id}");
            return;
        }
        s_processedMessageIds.Add(message.Id);
        // Broadcast logic...
    }
}
```

### 2. ISP-совместимые интерфейсы

```csharp
public interface IMessageSender : INetworkClient
{
    Task SendMessageAsync(string message, string sender);
    Task SendMessageAsync(string message, string sender, ChatType chatType, string? teamId = null);
}

public interface IMessageReceiver : INetworkClient
{
    ISubject<ChatMessage> OnMessageReceived { get; }
}

public interface IEventPublisher : INetworkClient
{
    Task RaiseEventAsync(EventType eventType, object data);
}
```

### 3. Mediator с Reflection-based Handler Dispatch

```csharp
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    var requestType = request.GetType();
    var responseType = typeof(TResponse);
    
    if (_handlers.TryGetValue((requestType, responseType), out var handler))
    {
        var method = typeof(IRequestHandler<,>)
            .MakeGenericType(requestType, responseType)
            .GetMethod("Handle");
            
        return (TResponse)await (Task<TResponse>)method.Invoke(handler, new[] { request });
    }
    
    throw new InvalidOperationException($"No handler registered for {requestType.Name}");
}
```

### 4. Enhanced NotificationBuilder

```csharp
public static NotificationBuilder ForKill(string killer, string victim)
{
    return new NotificationBuilder()
        .SetType(EventType.KillNotification)
        .SetMessage($"{killer} killed {victim}")
        .AddMetadata("killer", killer)
        .AddMetadata("victim", victim);
}
```

## Запуск и тестирование

### Требования:

- .NET 9.0 SDK
- Visual Studio 2022+ или VS Code

### Команды:

```bash
# Сборка проекта
dotnet build

# Запуск всех тестов (83 теста)
dotnet test ChatSystem.Tests/ChatSystem.Tests.csproj

# Запуск с покрытием кода
dotnet test ChatSystem.Tests/ChatSystem.Tests.csproj --collect:"XPlat Code Coverage"

# Запуск демо-приложения
dotnet run --project ChatSystem.Demo/ChatSystem.Demo.csproj
```

### Результаты тестирования:

- **Всего тестов**: 83
- **Прошло**: 83 ✅
- **Не прошло**: 0 ❌
- **Покрытие кода**: 89.51%
- **Время выполнения**: 13.39 секунд

### Детальный лог тестов:

```
Тестовый запуск для /Users/txcslm/FORK/CoreApplicantsRepo/ChatSystem.Tests/bin/Debug/net9.0/ChatSystem.Tests.dll (.NETCoreApp,Version=v9.0)
Версия VSTest 17.14.0 (arm64)

NUnit Adapter 4.6.0.0: Test execution started
Running all tests in /Users/txcslm/FORK/CoreApplicantsRepo/ChatSystem.Tests/bin/Debug/net9.0/ChatSystem.Tests.dll
   NUnit3TestExecutor discovered 83 of 83 NUnit test cases using Current Discovery mode, Non-Explicit run

[MockChatNetwork] Broadcasting message from TestSender: Test message (ID: b3fb938c-2b71-4ed5-acaf-5aaf4c9a219b, Type: Public)
[MessageProcessor] Pre-processing message from TestSender
[PublicMessageProcessor] Broadcasting public message: Test message
[MessageProcessor] Post-processing message from TestSender
[MessageProcessor] Successfully processed message from TestSender
[MockChatNetwork] Message delivered to 1 clients

[ConnectionLogger] Client TestClient state changed to: Connected
[ConnectionLogger] Client TestClient state changed to: Disconnected

[MockChatNetwork] Broadcasting message from Player1: Team A message (ID: e1b0eeea-112a-43c9-9276-d24d267de4da, Type: Team)
[MockChatNetwork] Message delivered to 2 clients

[MockChatNetwork] Client Client2 disconnected
[ConnectionLogger] Client Client2 state changed to: Disconnected
[MockChatNetwork] Client Client2 reconnected
[ConnectionLogger] Client Client2 state changed to: Connected
[MockChatNetwork] Broadcasting message from Player1: Hello after reconnect (ID: 0f60f573-6e24-46f5-8535-6f4ec9a48d07, Type: Public)
[MockChatNetwork] Message delivered to 2 clients

  ✅ EventReceived_AddsToEventsList [45 ms]
  ✅ MessageReceived_AddsToMessagesList [2 ms]
  ✅ MultipleEvents_AllStoredInOrder [< 1 ms]
  ✅ MultipleMessages_AllStoredInOrder [< 1 ms]
  ✅ SendChatMessageAsync_CallsNetworkSender [3 ms]
  ✅ SendNotificationAsync_CallsNetworkEventPublisher [< 1 ms]
  ✅ SendTeamMessageAsync_CallsMediatorWithCommand [2 ms]
  ✅ SendChatMessageAsync_IncludesProperDelayBetweenRetries [505 ms]
  ✅ TeamMessage_OnlyReachesTeamMembers [321 ms]
  ✅ ReconnectedClient_ReceivesMessagesAgain [336 ms]
  ✅ ExecuteAsync_WithFailingOperation_RetriesAndThrows [3 s]
  ✅ ExecuteAsync_VoidOperation_WithFailure_RetriesAndThrows [3 s]

Тестовый запуск выполнен.
Всего тестов: 83
     Пройдено: 83
 Общее время: 13,3928 Секунды
```

### Примеры тестов:

- **MockChatNetworkTests**: 15 тестов (broadcast, team filtering, disconnect/reconnect, authority logic)
- **ChatManagerExtendedTests**: 12 тестов (integration с mocks, reactive streams)
- **MediatorTests**: 14 тестов (command handling, notifications, handler registration)
- **NotificationBuilderTests**: 8 тестов (fluent API, metadata, factory methods)
- **IntegrationTests**: 12 тестов (end-to-end scenarios)
- **ConnectionStateTests**: 8 тестов (state management, observers)
- **MessageProcessorTests**: 6 тестов (preprocessing, filtering)
- **RetryStrategyTests**: 5 тестов (exponential backoff, failure handling)
- **NetworkFactoryTests**: 3 тестов (factory patterns)

## Демонстрация работы

Консольное демо показывает:

```
=== Chat System Demo ===

[MockChatNetwork] Broadcasting message from Player1: Hello everyone! (ID: 4cbb..., Type: Public)
[MockChatNetwork] Message delivered to 3 clients

[MockChatNetwork] Broadcasting message from Player1: Team A strategy (ID: c1a9..., Type: Team)  
[MockChatNetwork] Message delivered to 2 clients

[MockChatNetwork] Event raised: MatchStart, delivered to 3 clients

[MockChatNetwork] Client Player2 disconnected
[MockChatNetwork] Client Player2 reconnected
```

## Git Workflow

- **Ветка**: `feature/chat-system`
- **База**: `master`

## Соответствие требованиям

| Требование             | Статус | Описание                                                                 |
|------------------------|--------|--------------------------------------------------------------------------|
| Layered Architecture   | ✅      | Onion Architecture: Domain → Application → Infrastructure → Presentation |
| Dependency Injection   | ✅      | Microsoft.Extensions.DependencyInjection                                 |
| Builder Pattern        | ✅      | NotificationBuilder с метаданными и factory методами                     |
| Mediator Pattern       | ✅      | Полная реализация с reflection-based dispatch                            |
| System.Reactive        | ✅      | Subject/Observable для всех потоков событий                              |
| Async/await            | ✅      | Асинхронность везде                                                      |
| Broadcast сообщений    | ✅      | Мультиклиентский broadcast через список клиентов                         |
| Authority logic        | ✅      | Фильтрация дубликатов через HashSet                                      |
| Latency simulation     | ✅      | Task.Delay с randomization                                               |
| Disconnect/reconnect   | ✅      | Расширенная поддержка с логированием                                     |
| Team/Public chat       | ✅      | Фильтрация по ChatType и TeamId                                          |
| Multiple clients       | ✅      | Демо с 3 клиентами                                                       |
| Console.WriteLine логи | ✅      | Подробное логирование в MockChatNetwork                                  |
| Unit тесты NUnit+Moq   | ✅      | 83 теста, 89.51% покрытие                                                |
| Coverage >80%          | ✅      | 89.51% (превышает требование)                                            |

## Потраченное время

**Общее время**: ~5 часов

**Основные сложности**:
1. Переход от модульной к слоистой архитектуре потребовал значительного рефакторинга
2. Реализация reflection-based Mediator с правильной типизацией
3. Обеспечение thread-safety в multi-client MockChatNetwork
4. Балансирование покрытия тестов с качеством кода