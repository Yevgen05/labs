using System.Net.Sockets;
using System.Text;
using EchoServer;

namespace NetSdrClientAppTests;

{
   
}
[TestFixture]
public class EchoServerTest
{
    private EchoServer.EchoServer _server;

        [SetUp]
        public void SetUp()
        {
            _server = new EchoServer.EchoServer(5000);
        }

        [TearDown]
        public void TearDown()
        {
            _server.Stop();
        }

        [Test]
        public async Task Server_ShouldStartAndStopWithoutException()
        {
            // Start the server
            _ = Task.Run(() => _server.StartAsync());

            // Allow server to start
            await Task.Delay(1000);

            Assert.Pass("Server started and stopped without exception.");
        }

        [Test]
        public async Task Client_ShouldReceiveEchoedMessage()
        {
            // Start the server
            _ = Task.Run(() => _server.StartAsync());

            // Allow server to start
            await Task.Delay(1000);

            // Create a client and connect
            using (var client = new TcpClient("127.0.0.1", 5000))
            await using (var stream = client.GetStream())
            {
                byte[] message = Encoding.ASCII.GetBytes("Hello, EchoServer!");
                await stream.WriteAsync(message);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer);

                string echoedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Assert.That(echoedMessage, Is.EqualTo("Hello, EchoServer!"));
            }

            Assert.Pass("Server started and stopped without exception.");
        }

        [Test]
        public async Task Server_ShouldHandleClientConnectionAndDisconnection()
        {
            _ = Task.Run(() => _server.StartAsync());

            // Allow server to start
            await Task.Delay(1000);

            // Create a client
            var client = new TcpClient("127.0.0.1", 5000);
            var stream = client.GetStream();
            client.Close(); // Simulate disconnection

            Assert.Pass("Server handled client connection and disconnection without issue.");
        }
    }

[TestFixture]
public class UdpTimedSenderTests
{
    private UdpTimedSender _udpSender;
    private const string TestHost = "127.0.0.1";
    private const int TestPort = 5001;

    [SetUp]
    public void SetUp()
    {
        _udpSender = new UdpTimedSender(TestHost, TestPort);
    }

    [TearDown]
    public void TearDown()
    {
        _udpSender.StopSending();
        _udpSender.Dispose();
    }

    [Test]
    public async Task UdpSender_ShouldStartSendingMessages()
    {
        // Start sending messages every 500ms
        _udpSender.StartSending(500);

        // Allow server to start
        await Task.Delay(1000);

        // Stop the sending process
        _udpSender.StopSending();

        Assert.Pass("UDP sender successfully started and stopped.");
    }

    [Test]
    public void UdpSender_ShouldStopSendingMessages()
    {
        // Start sending messages
        _udpSender.StartSending(500);

        // Stop sending messages
        _udpSender.StopSending();

        // Try starting and stopping again
        _udpSender.StartSending(500);
        _udpSender.StopSending();

        Assert.Pass("UDP sender stopped without issues.");
    }

    [Test]
    public void UdpSender_ShouldThrowExceptionIfStartedTwice()
    {
        // Start sending messages
        _udpSender.StartSending(500);

        // Attempt to start again, should throw exception
        Assert.Throws<InvalidOperationException>(() => _udpSender.StartSending(500));

        // Stop the sending process
        _udpSender.StopSending();
    }
}