using System;

namespace BananaParty.WebSocketClient
{
    public class Socket : ISocket, IDisposable
    {
        private readonly string _serverAddress;

        private ISocket _webSocketClient;

        public Socket(string serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public bool IsConnected => _webSocketClient != null && _webSocketClient.IsConnected;

        public bool HasUnreadReceiveQueue => _webSocketClient != null ? _webSocketClient.HasUnreadReceiveQueue : throw new InvalidOperationException($"Trying to use {nameof(HasUnreadReceiveQueue)} before calling {nameof(Connect)}.");

        public byte[] ReadReceiveQueue() => _webSocketClient != null ? _webSocketClient.ReadReceiveQueue() : throw new InvalidOperationException($"Trying to use {nameof(ReadReceiveQueue)} before calling {nameof(Connect)}.");

        public void Connect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _webSocketClient = new BrowserSocket(_serverAddress);
#else
            _webSocketClient = new StandaloneSocket(_serverAddress);
#endif

            _webSocketClient.Connect();
        }

        public void Send(byte[] bytesToSend)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Trying to use {nameof(Send)} while not {nameof(IsConnected)}.");

            _webSocketClient.Send(bytesToSend);
        }

        public void Disconnect()
        {
            if (_webSocketClient == null)
                throw new InvalidOperationException($"Trying to use {nameof(Disconnect)} before calling {nameof(Connect)}.");

            _webSocketClient.Disconnect();
        }

        public void Dispose()
        {
            if (_webSocketClient != null)
                Disconnect();
        }
    }
}
