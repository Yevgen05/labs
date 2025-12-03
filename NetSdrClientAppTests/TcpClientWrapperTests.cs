using System.Net;
using System.Net.Sockets;
using System.Text;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private const string Host = "127.0.0.1";
        private const int Port = 5000;
        private TcpClientWrapper _client;
        private TcpListener _server;

        [SetUp]
        public void SetUp()
        {
            // Створення клієнта
            _client = new TcpClientWrapper(Host, Port);
            
            // Запуск серверу для тестування
            _server = new TcpListener(IPAddress.Parse(Host), Port);
            _server.Start();
            _ = AcceptClientsAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Dispose();
            _client.Disconnect();
        }

        
        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var tcpClient = await _server.AcceptTcpClientAsync();
                var networkStream = tcpClient.GetStream();
                byte[] buffer = new byte[8192];
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    var response = Encoding.UTF8.GetBytes("Received: " + Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    await networkStream.WriteAsync(response, 0, response.Length);
                }
            }
        }

        [Test]
        public void TestConnect_Disconnect()
        {
            _client.Connect();
            Assert.That(_client.Connected, Is.True, "Client should be connected.");
            
            _client.Disconnect();
            Assert.That(_client.Connected, Is.False,  "Client should be disconnected.");
        }
        
        [Test]
        public void TestDoubleConnect()
        {
            _client.Connect();
            _client.Connect();
            Assert.That(_client.Connected, Is.True, "Client should be connected.");
        }

        [Test]
        public async Task TestSendMessageAsync_WithByteArray()
        {
            _client.Connect();

            byte[] message = Encoding.UTF8.GetBytes("Hello Server");
            var messageReceived = false;
            
            // Підписка на подію отримання повідомлення
            _client.MessageReceived += (sender, data) =>
            {
                var response = Encoding.UTF8.GetString(data);
                if (response.Contains("Received: Hello Server"))
                {
                    messageReceived = true;
                }
            };

            await _client.SendMessageAsync(message);

            // Чекати, поки сервер не відповість
            await Task.Delay(1000);

            Assert.That(messageReceived, Is.True, "Server should receive the message and respond.");
        }

        [Test]
        public async Task TestSendMessageAsync_WithString()
        {
            _client.Connect();

            string message = "Hello Server";
            var messageReceived = false;
            
            // Підписка на подію отримання повідомлення
            _client.MessageReceived += (sender, data) =>
            {
                var response = Encoding.UTF8.GetString(data);
                if (response.Contains("Received: Hello Server"))
                {
                    messageReceived = true;
                }
            };

            await _client.SendMessageAsync(message);

            // Чекати, поки сервер не відповість
            await Task.Delay(1000);

            Assert.That(messageReceived, Is.True,"Server should receive the message and respond.");
        }
    }
}