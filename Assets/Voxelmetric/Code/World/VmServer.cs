using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class VmServer {

    protected World world;
    private Socket serverSocket;
    private Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();

    int nextId = 0;

    public VmServer(World world)
    {
        this.world = world;

        try
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Changed to GetHostAddresses from:
            //serverSocket.Bind(new IPEndPoint(Dns.Resolve(Dns.GetHostName()).AddressList[0], 11000));
            serverSocket.Bind(new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], 11000));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(new AsyncCallback(OnJoinServer), null);

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
            Socket newClientSocket = serverSocket.EndAccept(ar);
            ClientConnection connection = new ClientConnection(clients.Count, newClientSocket, this);
            clients.Add(nextId, connection);
            nextId++;

            serverSocket.BeginAccept(new AsyncCallback(OnJoinServer), null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private class ClientConnection
    {
        byte[] buffer = new byte[VmNetworking.bufferLength];
        public int id;
        Socket socket;
        VmServer server;

        public ClientConnection(int id, Socket socket, VmServer server)
        {
            this.id = id;
            this.socket = socket;
            this.server = server;
            Debug.Log("Client " + id + " has connected");

            socket.BeginReceive(buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromClient), null);
        }

        private void OnReceiveFromClient(IAsyncResult ar)
        {
            try
            {
                int received = socket.EndReceive(ar);

                if (received == 0)
                {
                    Disconnect();
                    return;
                }

                server.RouteMessageToFunction(buffer, id);

                socket.BeginReceive(buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromClient), null);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void Send(byte[] buffer)
        {
            try
            {
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void Disconnect()
        {
            Debug.Log("Client " + id + " has disconnected");
            socket.Close();
            server.clients[id] = null;
        }
    }

    public void DisconnectClients()
    {
        foreach (var client in clients.Values)
        {
            client.Disconnect();
        }
    }

    public void SendToClient(byte[] data, int client)
    {
        clients[client].Send(data);
    }

    public virtual void RouteMessageToFunction(byte[] receivedData, int id)
    {
        BlockPos pos;

        switch (receivedData[0])
        {
            case VmNetworking.SendBlockChange:
                pos = BlockPos.FromBytes(receivedData, 1);
                ReceiveChange(pos, Block.New(BitConverter.ToUInt16(receivedData, 13), world), id);
                break;
            case VmNetworking.RequestChunkData:
                pos = BlockPos.FromBytes(receivedData, 1);
                RequestChunk(pos, id);
                break;
        }
    }


    public void RequestChunk(BlockPos pos, int id)
    {
        //for now return an empty chunk if it isn't yet loaded
        // Todo: load the chunk then send it to the player
        if (world.chunks.Get(pos) == null)
        {
            Debug.LogError("Could not find chunk for " + pos);
            SendChunk(pos, new byte[16384], id);
        }
        else
        {
            byte[] data = world.chunks.Get(pos).blocks.ToBytes();
            SendChunk(pos, data, id);
        }
    }

    protected void SendChunk(BlockPos pos, byte[] chunkData, int id)
    {
        int chunkDataIndex = 0;
        while (chunkDataIndex < chunkData.Length)
        {
            byte[] message = new byte[1024];
            message[0] = VmNetworking.transmitChunkData;
            pos.ToBytes().CopyTo(message, 1);
            BitConverter.GetBytes(chunkData.Length).CopyTo(message, 13);

            for (int i = 17; i < message.Length; i++)
            {
                message[i] = chunkData[chunkDataIndex];
                chunkDataIndex++;

                if (chunkDataIndex >= chunkData.Length)
                {
                    break;
                }
            }

            SendToClient(message, id);
        }
    }

    public void BroadcastChange(BlockPos pos, Block block, int excludedUser)
    {
        if (clients.Count == 0)
        {
            return;
        }

        byte[] data = new byte[15];

        data[0] = VmNetworking.SendBlockChange;
        pos.ToBytes().CopyTo(data, 1);
        BitConverter.GetBytes(block.type).CopyTo(data, 13);

        foreach (var client in clients.Values)
        {
            if (excludedUser == -1 || client.id != excludedUser)
            {
                client.Send(data);
            }
        }
    }

    public void ReceiveChange(BlockPos pos, Block block, int id)
    {
        world.blocks.Set(pos, block, updateChunk: true, setBlockModified: false);
        BroadcastChange(pos, block, id);
    }
}
