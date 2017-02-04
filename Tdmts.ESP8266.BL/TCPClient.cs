using System;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Tdmts.ESP8266.BL
{

    public class TCPClient
    {
        private static TCPClient _instance;
        private StreamSocket _socket;

        public event EventHandler<string> OnConnect;
        public event EventHandler<string> OnSent;
        public event EventHandler<string> OnReceived;
        public event EventHandler<string> OnDisconnect;
        public event EventHandler<Exception> OnException;

        public static TCPClient getInstance()
        {
            if (_instance == null)
            {
                _instance = new TCPClient();
            }
            return _instance;
        }

        private TCPClient()
        {
            _socket = new StreamSocket();
        }

        public async void Connect(string host, string port)
        {
            try
            {
                HostName hostname = new HostName(host);
                await _socket.ConnectAsync(hostname, port, SocketProtectionLevel.PlainSocket);
                OnConnect(this, String.Format("Connected to host: {0} on port: {1}", host, port));
                Receive();
            }
            catch (Exception x)
            {
                OnException(this, x);
            }
        }

        public void Disconnect()
        {
            try
            {
                _socket.Dispose();
                OnDisconnect(this, "Disconnected from host");
            }
            catch (Exception x)
            {
                OnException(this, x);
            }
        }

        public async void Send(string message)
        {
            try
            {
                DataWriter _writer = new DataWriter(_socket.OutputStream);
                _writer.WriteString(message);
                await _writer.StoreAsync();
                _writer.DetachStream();
                OnSent(this, message);
            }
            catch (Exception x)
            {
                OnException(this, x);
            }
        }

        public async void Receive()
        {
            try
            {
                DataReader _reader = new DataReader(_socket.InputStream);
                uint header = await _reader.LoadAsync(4);

                if (header == 0)
                {
                    return;
                }

                int len = _reader.ReadInt32();
                uint lenBytes = await _reader.LoadAsync((uint)len);

                string message = _reader.ReadString(lenBytes);

                OnReceived(_socket, message);

                Receive();
            }
            catch (Exception x)
            {
                OnException(this, x);
            }
        }
    }
}
