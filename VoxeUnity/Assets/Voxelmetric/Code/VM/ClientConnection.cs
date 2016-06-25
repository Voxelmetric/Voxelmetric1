using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.VM
{
    internal class ClientConnection : VmSocketState.IMessageHandler
    {
        private Socket socket;
        private VmServer server;

        private bool debugClientConnection = false;

        public int ID { get; private set; }

        public ClientConnection(int ID, Socket socket, VmServer server)
        {
            this.ID = ID;
            this.socket = socket;
            this.server = server;
            if ( debugClientConnection )
                Debug.Log("ClientConnection.ClientConnection (" + Thread.CurrentThread.ManagedThreadId + "): "
                          + "Client " + ID + " has connected");

            VmSocketState socketState = new VmSocketState(this);
            socket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, OnReceiveFromClient, socketState);
        }

        private void OnReceiveFromClient(IAsyncResult ar)
        {
            try {
                if (socket == null || !socket.Connected)
                {
                    Debug.Log("ClientConnection.OnReceiveFromClient (" + Thread.CurrentThread.ManagedThreadId + "): "
                              + "client message rejected because connection was shutdown or not started");
                    return;
                }

                int received = socket.EndReceive(ar);
                if (received == 0)
                {
                    Disconnect();
                    return;
                }

                if (debugClientConnection)
                    Debug.Log("ClientConnection.OnReceiveFromClient (" + Thread.CurrentThread.ManagedThreadId + "): " + ID);

                VmSocketState socketState = ar.AsyncState as VmSocketState;
                socketState.Receive(received, 0);

                if (socket != null && socket.Connected)
                {
                    // Should be able to use a mutex but unity doesn't seem to like it
                    socket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, OnReceiveFromClient, socketState);
                }
            } catch (Exception ex) {
                Debug.LogError(ex);
            }
        }

        public int GetExpectedSize(byte type)
        {
            switch (type)
            {
                case VmNetworking.SendBlockChange:
                    return 17;
                case VmNetworking.RequestChunkData:
                    return 13;
                default:
                    return 0;
            }
        }

        public void HandleMessage(byte[] receivedData)
        {
            Vector3Int pos;

            switch (receivedData[0])
            {
                case VmNetworking.SendBlockChange:
                    pos = Vector3Int.FromBytes(receivedData, 1);
                    ushort type = BitConverter.ToUInt16(receivedData, 13);
                    server.ReceiveChange(pos, type, ID);
                    break;
                case VmNetworking.RequestChunkData:
                    pos = Vector3Int.FromBytes(receivedData, 1);

                    if (debugClientConnection)
                        Debug.Log("ClientConnection.HandleMessage (" + Thread.CurrentThread.ManagedThreadId + "): " + ID
                                  + " " + pos);

                    server.RequestChunk(pos, ID);
                    break;
            }
        }

        public void Send(byte[] buffer)
        {
            try {
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSend, socket);
            } catch (Exception ex) {
                Debug.LogError(ex);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try {
                socket.EndSend(ar);
            } catch (Exception ex) {
                Debug.LogError(ex);
            }
        }

        public void Disconnect()
        {
            if (debugClientConnection)
                Debug.Log("ClientConnection.Disconnect (" + Thread.CurrentThread.ManagedThreadId + "): "
                          + "Client " + ID + " has disconnected");
            try {
                if (socket != null) {// && socket.Connected) {
                    //socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket = null;
                }
            } catch (Exception ex) {
                Debug.LogError(ex);
            }
            server.RemoveClient(ID);
        }
    }
}