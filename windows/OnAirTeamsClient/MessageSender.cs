using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static OnAirTeamsClient.IStatusNotifier.Statuses;

namespace OnAirTeamsClient
{
    internal sealed class MessageSender
    {
        private const double HeartBeatTimeoutStartingValue = 10.0;

        private readonly string _hostname;
        private readonly int _port;
        private readonly Action<IStatusNotifier.Statuses> _serverStatusNotifier;
        private readonly ConcurrentQueue<string> _messageQueue;

        private IPEndPoint _remoteEndpoint;
        private AddressFamily _addressFamily;
        private double _heartbeatTimeout;

        public MessageSender(string hostname, int port, Action<IStatusNotifier.Statuses> serverStatusNotifier)
        {
            _hostname = hostname;
            _port = port;
            _serverStatusNotifier = serverStatusNotifier;
            _messageQueue = new ConcurrentQueue<string>();
            _heartbeatTimeout = HeartBeatTimeoutStartingValue;

            new Thread(SendMessageLoop) { IsBackground = true }.Start();
        }

        public void SendMessage(string message) => _messageQueue.Enqueue(message);

        private void SetupEndPointAndAddressFamilyIfRequired()
        {
            if (_remoteEndpoint != null) return;

            var ipHostInfo = Dns.GetHostEntry(_hostname);
            var ipAddress = ipHostInfo.AddressList[0];
            _remoteEndpoint = new IPEndPoint(ipAddress, _port);
            _addressFamily = ipAddress.AddressFamily;
        }

        private void SendMessageLoop()
        {
            while (true)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    var messageSent = false;
                    while (!messageSent)
                    {
                        try
                        {
                            SetupEndPointAndAddressFamilyIfRequired();

                            var sender = new Socket(_addressFamily, SocketType.Stream, ProtocolType.Tcp);
                            sender.Connect(_remoteEndpoint);

                            var msg = Encoding.ASCII.GetBytes($"{message}");
                            sender.Send(msg);

                            sender.Shutdown(SocketShutdown.Both);
                            sender.Close();

                            messageSent = true;
                            _serverStatusNotifier(On);
                        }
                        catch (Exception e) when (e is IndexOutOfRangeException || e is SocketException)
                        {
                            _serverStatusNotifier(Off);
                            Thread.Sleep(2000);
                        }
                    }

                    _heartbeatTimeout = HeartBeatTimeoutStartingValue;
                }
                else
                {
                    _heartbeatTimeout -= 0.1;

                    if (_heartbeatTimeout <= 0.0)
                    {
                        _messageQueue.Enqueue("Heartbeat");
                        _heartbeatTimeout = HeartBeatTimeoutStartingValue;
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
