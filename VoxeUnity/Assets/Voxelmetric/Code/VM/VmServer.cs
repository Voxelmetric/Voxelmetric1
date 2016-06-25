using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.VM
{
    public class VmServer
    {
        protected World world;
        private IPAddress serverIP;
        private Socket serverSocket;

        private Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
        private int nextId = 0;

        private bool debugServer = false;

        public IPAddress ServerIP { get { return serverIP; } }

        public int ClientCount
        {
            get {
                lock (clients)
                {
                    return clients.Count;
                }
            }
        }

        public VmServer(World world)
        {
            this.world = world;

            try
            {
                AddressFamily addressFamily = AddressFamily.InterNetwork;
                serverSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

                string serverName = Dns.GetHostName();
                if (debugServer) Debug.Log("serverName='" + serverName + "'");
                foreach (IPAddress serverAddress in Dns.GetHostAddresses(serverName)) {
                    if (debugServer) Debug.Log("serverAddress='" + serverAddress + "', AddressFamily=" + serverAddress.AddressFamily);
                    if (serverAddress.AddressFamily !=  addressFamily)
                        continue;
                    serverIP = serverAddress;
                    break;
                }
                IPEndPoint serverEndPoint = new IPEndPoint(serverIP, 8000);
                serverSocket.Bind(serverEndPoint);
                serverSocket.Listen(0);
                serverSocket.BeginAccept(OnJoinServer, null);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnJoinServer(IAsyncResult ar)
        {
            try
            {
                if (serverSocket == null) {
                    Debug.Log("VmServer.OnJoinServer (" + Thread.CurrentThread.ManagedThreadId + "): "
                              + "client connection rejected because server was not started");
                    return;
                }
                Socket newClientSocket = serverSocket.EndAccept(ar);
                lock(clients) {
                    ClientConnection connection = new ClientConnection(clients.Count, newClientSocket, this);
                    clients.Add(nextId, connection);
                    nextId++;
                }

                serverSocket.BeginAccept(OnJoinServer, null);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        internal void RemoveClient(int id)
        {
            lock(clients)
            {
                clients[id] = null;
            }
        }

        public void Disconnect()
        {
            lock(clients)
            {
                var clientConnections = clients.Values.ToList();
                foreach (var client in clientConnections)
                    client.Disconnect();
            }

            if (serverSocket != null) {// && serverSocket.Connected) {
                //serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
                serverSocket = null;
            }
        }

        public void SendToClient(byte[] data, int client)
        {
            lock (clients)
            {
                ClientConnection clientConnection = clients[client];
                if ( clientConnection != null )
                    clientConnection.Send(data);
            }
        }

        public void RequestChunk(Vector3Int pos, int id)
        {
            Chunk chunk = null;
            if (world == null)
            {
                Debug.LogError("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): "
                               + " world not set (" + pos + ", " + id + ")");
            }
            else
                chunk = world.chunks.Get(pos);

            byte[] data;

            //for now return an empty chunk if it isn't yet loaded
            // Todo: load the chunk then send it to the player
            if (chunk == null)
            {
                Debug.LogError("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): "
                               + "Could not find chunk for " + pos);
                data = ChunkBlocks.EmptyBytes;
            }
            else
                data = chunk.blocks.ToBytes();

            if ( debugServer )
                Debug.Log("VmServer.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): " + id
                          + " " + pos);

            SendChunk(pos, data, id);
        }

        public const int headerSize = 13, leaderSize = headerSize + 8;

        protected void SendChunk(Vector3Int pos, byte[] chunkData, int id)
        {
            int chunkDataIndex = 0;
            while (chunkDataIndex < chunkData.Length)
            {
                byte[] message = new byte[VmNetworking.bufferLength];
                message[0] = VmNetworking.transmitChunkData;
                pos.ToBytes().CopyTo(message, 1);
                BitConverter.GetBytes(chunkDataIndex).CopyTo(message, headerSize);
                BitConverter.GetBytes(chunkData.Length).CopyTo(message, headerSize + 4);

                if ( debugServer )
                    Debug.Log("VmServer.SendChunk (" + Thread.CurrentThread.ManagedThreadId + "): " + pos
                              + ", chunkDataIndex=" + chunkDataIndex
                              + ", chunkData.Length=" + chunkData.Length
                              + ", buffer=" + message.Length);

                for (int i = leaderSize; i < message.Length; i++)
                {
                    message[i] = chunkData[chunkDataIndex];
                    chunkDataIndex++;

                    if (chunkDataIndex >= chunkData.Length)
                        break;
                }

                SendToClient(message, id);
            }
        }

        public void BroadcastChange(Vector3Int pos, BlockData blockData, int excludedUser)
        {
            lock(clients)
            {
                if (clients.Count == 0)
                    return;

                byte[] data = new byte[15];

                data[0] = VmNetworking.SendBlockChange;
                pos.ToBytes().CopyTo(data, 1);
                BitConverter.GetBytes(blockData.Type).CopyTo(data, 13);

                foreach (var client in clients.Values)
                {
                    if (excludedUser == -1 || client.ID != excludedUser)
                        client.Send(data);
                }
            }
        }

        public void ReceiveChange(Vector3Int pos, ushort type, int id)
        {
            BlockData blockData = new BlockData(type);
            world.blocks.Modify(pos, blockData, true);
            BroadcastChange(pos, blockData, id);
        }
    }
}