using ChatSystem.Infrastructure.Network;

namespace ChatSystem.Tests;

[TestFixture]
public class NetworkFactoryTests
{
  [SetUp]
  public void Setup()
  {
    _mockFactory = new MockChatNetworkFactory();
    _highLatencyFactory = new HighLatencyNetworkFactory();
  }

  [TearDown]
  public void TearDown()
  {
    MockChatNetwork.ClearAllClients();
  }

  private MockChatNetworkFactory _mockFactory = null!;
  private HighLatencyNetworkFactory _highLatencyFactory = null!;

  [Test]
  public void MockNetworkFactory_CreateClient_WithDefaultLatency()
  {
    var client = _mockFactory.CreateClient("TestClient1");

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("TestClient1"));
  }

  [Test]
  public void MockNetworkFactory_CreateClient_WithCustomLatency()
  {
    var client = _mockFactory.CreateClient("TestClient2", 500);

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("TestClient2"));
  }

  [Test]
  public void MockNetworkFactory_CreateClient_WithTeamId()
  {
    var client = (MockChatNetwork)_mockFactory.CreateClient("TestClient3", 200, "TeamA");

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("TestClient3"));
  }

  [Test]
  public void HighLatencyNetworkFactory_CreateClient_WithDefaultLatency()
  {
    var client = _highLatencyFactory.CreateClient("HighLatencyClient1");

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("HighLatencyClient1"));
  }

  [Test]
  public void HighLatencyNetworkFactory_CreateClient_WithCustomLatency()
  {
    var client = _highLatencyFactory.CreateClient("HighLatencyClient2", 500);

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("HighLatencyClient2"));
  }

  [Test]
  public void HighLatencyNetworkFactory_CreateClient_EnforcesMinimumLatency()
  {
    var client = _highLatencyFactory.CreateClient("HighLatencyClient3", 100);

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("HighLatencyClient3"));
  }

  [Test]
  public void HighLatencyNetworkFactory_CreateClient_WithTeamId()
  {
    var client = (MockChatNetwork)_highLatencyFactory.CreateClient("HighLatencyClient4", 1200, "TeamB");

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("HighLatencyClient4"));
  }

  [Test]
  public void NetworkFactory_CreateMultipleClients_AllUnique()
  {
    var client1 = _mockFactory.CreateClient("Client1");
    var client2 = _mockFactory.CreateClient("Client2");
    var client3 = _highLatencyFactory.CreateClient("Client3");

    Assert.That(client1.ClientId, Is.Not.EqualTo(client2.ClientId));
    Assert.That(client2.ClientId, Is.Not.EqualTo(client3.ClientId));
    Assert.That(client1.ClientId, Is.Not.EqualTo(client3.ClientId));
  }

  [Test]
  public void NetworkFactory_InterfaceAbstraction_WorksCorrectly()
  {
    IChatNetworkFactory factory = _mockFactory;

    var client = factory.CreateClient("InterfaceTestClient");

    Assert.That(client, Is.Not.Null);
    Assert.That(client.ClientId, Is.EqualTo("InterfaceTestClient"));
  }
}