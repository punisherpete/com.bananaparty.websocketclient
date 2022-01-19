using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BananaParty.WebSocketClient
{
    public class StandaloneSocket : ISocket
    {
        private const int TransportMaxReceiveChunkSize = 1024;
        private const int TransportMaxSendChunkSize = 1024;

        private readonly Uri _serverUri;

        private readonly ClientWebSocket _clientWebSocket = new();
        private readonly CancellationTokenSource _disconnectTokenSource = new();

        private readonly Queue<byte[]> _receiveQueue = new();

        public bool IsConnected => _clientWebSocket.State == WebSocketState.Open;

        public bool HasUnreadReceiveQueue => _receiveQueue.Count > 0;

        public byte[] ReadReceiveQueue() => _receiveQueue.Dequeue();

        public StandaloneSocket(string serverAddress)
        {
            _serverUri = new Uri(serverAddress);
        }

        public void Connect()
        {
            ConnectAndReceiveLoopAsync();
        }

        public void Disconnect()
        {
            _disconnectTokenSource.Cancel();
        }

        public void Send(byte[] bytesToSend)
        {
            SendAsync(bytesToSend);
        }

        private async void SendAsync(byte[] bytesToSend)
        {
            if (!IsConnected)
                throw new InvalidOperationException($"Connection is not open. State = {_clientWebSocket.State}");

            int bytesSent = 0;
            while (bytesSent < bytesToSend.Length)
            {
                var bytesSegment = new ArraySegment<byte>(bytesToSend, bytesSent, Math.Min(bytesToSend.Length - bytesSent, TransportMaxSendChunkSize));
                bool isFinalChunk = bytesSegment.Offset + bytesSegment.Count >= bytesToSend.Length;
                await _clientWebSocket.SendAsync(bytesSegment, WebSocketMessageType.Binary, isFinalChunk, _disconnectTokenSource.Token);
                bytesSent += bytesSegment.Count;
            }
        }

        private async void ConnectAndReceiveLoopAsync()
        {
            Task connectTask = _clientWebSocket.ConnectAsync(_serverUri, _disconnectTokenSource.Token);

            // Workaround for "ObjectDisposedException: Cannot access a disposed object".
            while (!connectTask.IsCompleted)
            {
                await Task.Yield();

                if (_disconnectTokenSource.IsCancellationRequested)
                    goto ConnectionAborted;
            }

            if (!connectTask.IsCompletedSuccessfully)
                goto ConnectionAborted;

            var receiveBuffer = new ArraySegment<byte>(new byte[TransportMaxReceiveChunkSize]);
            WebSocketReceiveResult result;
            do
            {
                var arrayBufferWriter = new ArrayBufferWriter<byte>();
                do
                {
                    Task<WebSocketReceiveResult> receiveTask = _clientWebSocket.ReceiveAsync(receiveBuffer, _disconnectTokenSource.Token);
                    // Workaround for bug where it awaits forever if server connection is gone.
                    while (!receiveTask.IsCompleted)
                    {
                        await Task.Yield();

                        // Workaround for "The WebSocket is in an invalid state ('Aborted') for this operation".
                        if (_clientWebSocket.State == WebSocketState.Aborted)
                            goto ConnectionAborted;
                    }

                    // Workaround for "Operation was cancelled".
                    if (_disconnectTokenSource.IsCancellationRequested)
                        goto DisconnectRequested;

                    result = receiveTask.Result;

                    arrayBufferWriter.Write(receiveBuffer.Slice(0, result.Count));
                }
                while (!result.EndOfMessage);

                _receiveQueue.Enqueue(arrayBufferWriter.WrittenSpan.ToArray());
            }
            while (result.MessageType != WebSocketMessageType.Close);

        DisconnectRequested:
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

        ConnectionAborted:
            _clientWebSocket.Dispose();
        }
    }
}
