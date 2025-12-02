using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServerApp
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public class EchoServer : IDisposable
    {
        private readonly int _port;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public EchoServer(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token), 
                        _cancellationTokenSource.Token);
                }
            }
            catch (ObjectDisposedException)
            {
                // Listener has been closed
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested
            }

            Console.WriteLine("Server shutdown.");
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && 
                           (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead, token);
                        Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Dispose();
                    _listener?.Stop();
                }
                _disposed = true;
            }
        }

        public static async Task Main(string[] args)
        {
            using EchoServer server = new EchoServer(5000);

            var serverTask = server.StartAsync();

            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;

            using (var sender = new UdpTimedSender(host, port))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                Console.WriteLine("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Wait for 'q'
                }

                sender.StopSending();
                server.Stop();
                Console.WriteLine("Sender stopped.");
            }

            await serverTask;
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private Timer? _timer;
        private ushort _counter = 0;
        private bool _disposed;

        public UdpTimedSender(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private void SendMessageCallback(object? state)
        {
            try
            {
                byte[] samples = new byte[1024];
                Random.Shared.NextBytes(samples);
                _counter++;

                byte[] counterBytes = BitConverter.GetBytes(_counter);
                byte[] msg = new byte[2 + counterBytes.Length + samples.Length];
                
                msg[0] = 0x04;
                msg[1] = 0x84;
                
                Buffer.BlockCopy(counterBytes, 0, msg, 2, counterBytes.Length);
                Buffer.BlockCopy(samples, 0, msg, 2 + counterBytes.Length, samples.Length);
                
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopSending();
                    _udpClient.Dispose();
                }
                _disposed = true;
            }
        }
    }
}