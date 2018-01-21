using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ComSocketBridge
{
    class TcpManager
    {
        private TcpListener _serverSocket;
        private readonly int _localport = 0;
        private static int _counter = 0;
        private static object _lock = new Object(); // sync lock 
        private readonly string _greeting = "";
        private static List<SockTcpClient> ConnectedClients = new List<SockTcpClient>();
        private static List<Task> _connections = new List<Task>(); // pending connections

        public TcpListener ServerSocket => _serverSocket;
        public OnReceivedFromClientDelegate OnReceivedFromClientDelegate;
        public int LocalPort => _localport;
        internal TcpOnListeningStartedHandler OnListeningStarted;
        internal TcpOnListeningFailHandler OnListeningFailed;

        public TcpManager(int localPort, string greeting, OnReceivedFromClientDelegate onReceivedFromClientDelegate = null)
        {
            _localport = localPort;
            _greeting = greeting;

            if (onReceivedFromClientDelegate != null)
                OnReceivedFromClientDelegate += onReceivedFromClientDelegate;
        }

        // Register and handle the connection
        private async Task StartHandleConnectionAsync(SockTcpClient sockTcpClient)
        {
            lock (_lock)
            {
                SendMessage(sockTcpClient, _greeting);
                _counter++;
                //SockTcpClient sockTcpClient = new SockTcpClient(sockTcpClient, _counter, ((IPEndPoint)sockTcpClient.Client.RemoteEndPoint).Address.ToString());
                Logger.Log($"Client Connected: #{Convert.ToString(sockTcpClient.ClientNumber)} ({sockTcpClient.ClientAddress})");
                ConnectedClients.Add(sockTcpClient);
                Logger.Log($"Clients Count: {ConnectedClients.Count}");
            }

            // start the new connection task
            var connectionTask = HandleConnectionAsync(sockTcpClient);

            // add it to the list of pending task 
            lock (_lock)
            {
                _connections.Add(connectionTask);
            }

            // catch all errors of HandleConnectionAsync
            try
            {
                await connectionTask;
                // we may be on another thread after "await"
            }
            catch (Exception ex)
            {
                // Logger.Log the error
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                // remove pending task
                lock (_lock)
                    _connections.Remove(connectionTask);
            }
        }

        public Task StartListener()
        {
            return Task.Run(async () =>
            {
                // Start Server SOCKET!
                _counter = 0;

                try
                {
                    _serverSocket = new TcpListener(IPAddress.Any, _localport);
                }
                catch (Exception e)
                {
                    OnListeningFailed?.Invoke(this, new MessengerEventArgs(e.Message));
                    return;
                }

                try
                {
                    _serverSocket.Start();
                    OnListeningStarted?.Invoke(this, new MessengerEventArgs("Listening on Port " + _localport));
                }
                catch (SocketException e)
                {
                    OnListeningFailed?.Invoke(this, new MessengerEventArgs(e.Message));
                    return;
                }

                while (true)
                {
                    _counter++;
                    var tcpClient = await _serverSocket.AcceptTcpClientAsync();
                    SockTcpClient s = new SockTcpClient(tcpClient, _counter, ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
                    var task = StartHandleConnectionAsync(s);

                    // if already faulted, re-throw any error on the calling context
                    if (task.IsFaulted)
                        task.Wait();
                }
            });
        }

        // Handle new connection
        private Task HandleConnectionAsync(SockTcpClient sockTcpClient)
        {
            string receivedBuffer = "";
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            return Task.Run(async () =>
            {
                using (NetworkStream networkStream = sockTcpClient.ClientSocket.GetStream())
                {
                    while (true)
                    {
                        bool isDisconnected = false;
                        string dataFromClient = "";
                        var bytesFrom = new byte[sockTcpClient.ClientSocket.ReceiveBufferSize];

                        try
                        {
                            networkStream.Read(bytesFrom, 0, (int)sockTcpClient.ClientSocket.ReceiveBufferSize);
                        }
                        catch
                        {
                            isDisconnected = true;
                        }
                        
                        if (isDisconnected == false)
                        {
                            dataFromClient = Encoding.ASCII.GetString(bytesFrom);
                            dataFromClient = dataFromClient.Replace("\0", "");
                            if (dataFromClient.Length <= 0)
                                isDisconnected = true;
                        }

                        if (isDisconnected)
                        {
                            Logger.Log($"Client Disconnected [Received 0 length buffer] (#{sockTcpClient.ClientNumber}, IP:{sockTcpClient.ClientAddress})");
                            ConnectedClients.Remove(sockTcpClient);
                            receivedBuffer = "";
                            sockTcpClient.ClientSocket.Close();
                            break;
                        }
                        else
                        {
                            receivedBuffer += dataFromClient;
                            //if (receivedBuffer.Contains("\r"))
                            //{
                            Logger.Log($"Received from client {sockTcpClient.ClientNumber}: {receivedBuffer}");
                            OnReceivedFromClient(receivedBuffer);
                            receivedBuffer = "";
                            //}
                        }
                    }

                }
            });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        }

        private void OnReceivedFromClient(string msg)
        {
            OnReceivedFromClientDelegate?.Invoke(this, new MessengerEventArgs(msg));
        }

        private static SockTcpClient SendMessage(SockTcpClient clientSocket, string _message)
        {
            try
            {
                byte[] message = Encoding.ASCII.GetBytes(_message.Trim() + "\r\n");
                clientSocket.ClientSocket.GetStream().WriteAsync(message, 0, message.Length);
                clientSocket.ClientSocket.GetStream().Flush();
            }
            catch (Exception)
            {
                return clientSocket;
            }

            return null;
        }

        public static void Broadcast(object sender, MessengerEventArgs args)
        {
            Broadcast(args.Message);
        }

        public static void Broadcast(string msg)
        {
            ArrayList toBeRemoved = new ArrayList();
            foreach (SockTcpClient c in ConnectedClients)
            {
                SockTcpClient failed = SendMessage(c, msg);
                if (failed != null)
                    toBeRemoved.Add(failed);
            }

            for (int i = toBeRemoved.Count - 1; i >= 0; i--)
            {
                SockTcpClient failedClient = toBeRemoved[i] as SockTcpClient;
                if (failedClient != null)
                {
                    toBeRemoved.RemoveAt(i);
                    ConnectedClients.Remove(failedClient);
                    Logger.Log($"Client Disconnected (IP:{failedClient.ClientAddress})");
                }
            }
        }

        public class SockTcpClient
        {
            private readonly TcpClient _clientSocket;
            private readonly int _clientNumber;
            private readonly string _clientAddress;

            public TcpClient ClientSocket => _clientSocket;
            public int ClientNumber => _clientNumber;
            public string ClientAddress => _clientAddress;

            public SockTcpClient(TcpClient s, int cNum, string address)
            {
                _clientSocket = s;
                _clientNumber = cNum;
                _clientAddress = address;
            }

        }
    }
}
