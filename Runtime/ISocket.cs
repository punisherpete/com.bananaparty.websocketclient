namespace BananaParty.WebSocketClient
{
    public interface ISocket
    {
        bool IsConnected { get; }

        bool HasUnreadReceiveQueue { get; }

        byte[] ReadReceiveQueue();

        /// <summary>
        /// Operation is not immediate. Check <see cref="IsConnected"/> for connection status.
        /// </summary>
        void Connect();

        void Send(byte[] bytesToSend);

        /// <summary>
        /// Operation is not immediate. Check <see cref="IsConnected"/> for connection status.
        /// </summary>
        void Disconnect();
    }
}
