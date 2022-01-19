using System.Runtime.InteropServices;

namespace BananaParty.WebSocketClient
{
    public class BrowserSocket : ISocket
    {
        private readonly string _serverAddress;

        private int _socketIndex = -1;

        public BrowserSocket(string serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public bool IsConnected => GetBrowserSocketIsConnected(_socketIndex);

        [DllImport("__Internal")]
        private static extern bool GetBrowserSocketIsConnected(int socketIndex);

        public bool HasUnreadReceiveQueue => GetBrowserSocketHasUnreadReceiveQueue(_socketIndex);

        [DllImport("__Internal")]
        private static extern bool GetBrowserSocketHasUnreadReceiveQueue(int socketIndex);

        public byte[] ReadReceiveQueue()
        {
            int receivedBytesCount = BrowserSocketReadReceiveQueue(_socketIndex, null, 0);
            byte[] _receivedBytesBuffer = new byte[receivedBytesCount];
            BrowserSocketReadReceiveQueue(_socketIndex, _receivedBytesBuffer, _receivedBytesBuffer.Length);
            return _receivedBytesBuffer;
        }

        /// <summary>
        /// Does not remove item from the queue if it's not going to fit in <paramref name="byteBufferLength"/>.
        /// </summary>
        /// <returns>Received bytes count.</returns>
        [DllImport("__Internal")]
        private static extern int BrowserSocketReadReceiveQueue(int socketIndex, byte[] byteBuffer, int byteBufferLength);

        public void Connect()
        {
            _socketIndex = BrowserSocketConnect(_serverAddress);
        }

        [DllImport("__Internal")]
        private static extern int BrowserSocketConnect(string serverAddress);

        public void Send(byte[] bytesToSend)
        {
            BrowserSocketSend(_socketIndex, bytesToSend, bytesToSend.Length);
        }

        [DllImport("__Internal")]
        private static extern void BrowserSocketSend(int socketIndex, byte[] bytesToSend, int bytesToSendCount);

        public void Disconnect()
        {
            BrowserSocketDisconnect(_socketIndex);
        }

        [DllImport("__Internal")]
        private static extern void BrowserSocketDisconnect(int socketIndex);
    }
}
