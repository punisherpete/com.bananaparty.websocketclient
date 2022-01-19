const library = {

  // Class definition.

  $browserSocket: {
    sockets: [],

    getBrowserSocketIsConnected: function (socketIndex) {
      return browserSocket.sockets[socketIndex].webSocket.readyState === WebSocket.OPEN;
    },

    getBrowserSocketHasUnreadReceiveQueue: function (socketIndex) {
      return browserSocket.sockets[socketIndex].receiveQueue.length > 0;
    },

    browserSocketReadReceiveQueue: function (socketIndex, byteBufferPtr, byteBufferLength) {
      const receivedBytesLength = browserSocket.sockets[socketIndex].receiveQueue[0].length;
      if (byteBufferLength < receivedBytesLength)
        return receivedBytesLength;

      const receivedBytes = browserSocket.sockets[socketIndex].receiveQueue.shift();
      HEAPU8.set(receivedBytes, byteBufferPtr);
      return receivedBytesLength;
    },

    browserSocketConnect: function (serverAddress) {
      const webSocket = new WebSocket(serverAddress);
      webSocket.binaryType = 'arraybuffer';

      const receiveQueue = [];

      webSocket.onmessage = function (messageEvent) {
        if (messageEvent.data instanceof ArrayBuffer) {
          receiveQueue.push(new Uint8Array(messageEvent.data));
        } else if (typeof messageEvent.data === 'string') {
          receiveQueue.push(new TextEncoder().encode(messageEvent.data));
        } else if (messageEvent.data instanceof Blob) {
          console.error('Blob message type not supported. messageEvent.data=' + messageEvent.data);
        } else {
          console.error('Unknown message type not supported. messageEvent.data=' + messageEvent.data);
        }
      }

      const socket = { webSocket: webSocket, receiveQueue: receiveQueue };

      const socketIndex = browserSocket.sockets.push(socket) - 1;
      return socketIndex;
    },

    browserSocketSend: function (socketIndex, bytesToSend) {
      browserSocket.sockets[socketIndex].webSocket.send(bytesToSend);
    },

    browserSocketDisconnect: function (socketIndex) {
      browserSocket.sockets[socketIndex].webSocket.close();
    }
  },


  // External C# calls.

  GetBrowserSocketIsConnected: function (socketIndex) {
    return browserSocket.getBrowserSocketIsConnected(socketIndex);
  },

  GetBrowserSocketHasUnreadReceiveQueue: function (socketIndex) {
    return browserSocket.getBrowserSocketHasUnreadReceiveQueue(socketIndex);
  },

  BrowserSocketReadReceiveQueue: function (socketIndex, byteBufferPtr, byteBufferLength) {
    return browserSocket.browserSocketReadReceiveQueue(socketIndex, byteBufferPtr, byteBufferLength);
  },

  BrowserSocketConnect: function (serverAddressPtr) {
    const serverAddress = UTF8ToString(serverAddressPtr);
    return browserSocket.browserSocketConnect(serverAddress);
  },

  BrowserSocketSend: function (socketIndex, bytesToSendPtr, bytesToSendCount) {
    const bytesToSend = HEAPU8.buffer.slice(bytesToSendPtr, bytesToSendPtr + bytesToSendCount);
    browserSocket.browserSocketSend(socketIndex, bytesToSend);
  },

  BrowserSocketDisconnect: function (socketIndex) {
    browserSocket.browserSocketDisconnect(socketIndex);
  },
};

autoAddDeps(library, '$browserSocket');
mergeInto(LibraryManager.library, library);
