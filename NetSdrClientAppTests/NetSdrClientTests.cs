using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
   NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    //TODO: cover the rest of the NetSdrClient code here
}

// ============================
// ДОДАНО НОВІ ТЕСТИ
// ============================

[TestFixture]
public class TcpClientWrapperTests
{
    [Test]
    public void Constructor_WithHostAndPort_ShouldInitializeCorrectly()
    {
        // Arrange
        string host = "localhost";
        int port = 8080;

        // Act
        var wrapper = new TcpClientWrapper(host, port);

        // Assert
        Assert.IsNotNull(wrapper);
    }

    [Test]
    public void Connected_WhenNotConnected_ShouldReturnFalse()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("localhost", 8080);

        // Act
        bool connected = wrapper.Connected;

        // Assert
        Assert.IsFalse(connected);
    }

    [Test]
    public void SendMessageAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("localhost", 8080);
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapper.SendMessageAsync(data)
        );
    }

    [Test]
    public void Disconnect_WhenNotConnected_ShouldNotThrowException()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("localhost", 8080);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Disconnect());
    }
}

[TestFixture]
public class UdpClientWrapperTests
{
    [Test]
    public void Constructor_WithPort_ShouldInitialize()
    {
        // Arrange
        int testPort = 5000;

        // Act
        var wrapper = new UdpClientWrapper(testPort);

        // Assert
        Assert.IsNotNull(wrapper);
    }

    [Test]
    public void GetHashCode_ForSamePort_ShouldReturnSameValue()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(5000);
        var wrapper2 = new UdpClientWrapper(5000);

        // Act
        int hash1 = wrapper1.GetHashCode();
        int hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [Test]
    public void GetHashCode_ForDifferentPorts_ShouldReturnDifferentValues()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(5000);
        var wrapper2 = new UdpClientWrapper(5001);

        // Act
        int hash1 = wrapper1.GetHashCode();
        int hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [Test]
    public void StopListening_WhenNotStarted_ShouldNotThrowException()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(5000);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.StopListening());
    }
}

[TestFixture]
public class InterfaceTests
{
    [Test]
    public void TcpClientWrapper_Implements_ITcpClient()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("localhost", 8080);

        // Act & Assert
        Assert.IsInstanceOf<ITcpClient>(wrapper);
    }

    [Test]
    public void UdpClientWrapper_Implements_IUdpClient()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(5000);

        // Act & Assert
        Assert.IsInstanceOf<IUdpClient>(wrapper);
    }
}

[TestFixture]
[Category("Integration")]
public class NetworkingIntegrationTests
{
    [Test]
    public void TcpClientWrapper_CanBeInstantiated()
    {
        // Arrange & Act
        var wrapper = new TcpClientWrapper("127.0.0.1", 8080);

        // Assert
        Assert.IsNotNull(wrapper);
        Assert.IsFalse(wrapper.Connected);
    }

    [Test]
    public void UdpClientWrapper_CanBeInstantiated()
    {
        // Arrange & Act
        var wrapper = new UdpClientWrapper(5000);

        // Assert
        Assert.IsNotNull(wrapper);
    }
}
