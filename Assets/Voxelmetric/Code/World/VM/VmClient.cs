using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class VmClient : VmSocketState.IMessageHandler
{
    protected World world;
    private IPAddress serverIP;
    private Socket clientSocket;

    public bool connected;

    private bool debugClient = false;

    public IPAddress ServerIP { get { return serverIP; } }

    public VmClient(World world, IPAddress serverIP = null)
    {
        this.world = world;
        this.serverIP = serverIP;
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        if (serverIP == null) {
            string serverName = Dns.GetHostName();
            Debug.Log("serverName='" + serverName + "'");
            IPAddress serverAddress = Dns.GetHostAddresses(serverName)[0];
            Debug.Log("serverAddress='" + serverAddress + "'");
            serverIP = serverAddress;
        }
        IPEndPoint serverEndPoint = new IPEndPoint(serverIP, 8000);

        clientSocket.BeginConnect(serverEndPoint, new AsyncCallback(OnConnect), null);
    }
    
    private void OnConnect(IAsyncResult ar)
    {
        try {

            if (clientSocket == null || !clientSocket.Connected) {
                Debug.Log("VmClient.OnConnect (" + Thread.CurrentThread.ManagedThreadId + "): "
                    + "server connection rejected because connection was shutdown or not started");
                return;
            }
            clientSocket.EndConnect(ar);
            connected = true;

            VmSocketState socketState = new VmSocketState(this);
            clientSocket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromServer), socketState);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void OnReceiveFromServer(IAsyncResult ar)
    {
        try
        {
            if (clientSocket == null || !clientSocket.Connected) {
                Debug.Log("VmClient.OnReceiveFromServer (" + Thread.CurrentThread.ManagedThreadId + "): "
                    + "server message rejected because connection was shutdown or not started");
                return;
            }
            int received = clientSocket.EndReceive(ar);

            if (received == 0)
            {
                Debug.Log("disconnected from server");
                Disconnect();
                return;
            }

            if ( debugClient )
                Debug.Log("VmClient.OnReceiveFromServer (" + Thread.CurrentThread.ManagedThreadId + "): ");

            VmSocketState socketState = ar.AsyncState as VmSocketState;
            socketState.Receive(received, 0);
            if (clientSocket != null && clientSocket.Connected) { // Should be able to use a mutex but unity doesn't seem to like it
                clientSocket.BeginReceive(socketState.buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromServer), socketState);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void Send(byte[] buffer)
    {
        if (!connected) {
            return;
        }
        try
        {
            clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
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
            clientSocket.EndSend(ar);
            if ( debugClient )
                Debug.Log("VmClient.OnSend (" + Thread.CurrentThread.ManagedThreadId + "): send ended");
        } catch (Exception ex) 
        {
            Debug.LogError(ex);
            Disconnect();
        }
    }

    public int GetExpectedSize(byte messageType) {
        switch (messageType) {
            case VmNetworking.SendBlockChange:
                return 15;
            case VmNetworking.transmitChunkData:
                //TODO TCD So that small chunks don't need 1025 bytes to be sent...
                //return -VmServer.leaderSize;
                return VmNetworking.bufferLength;
            default:
                return 0;
        }
    }

    public void HandleMessage(byte[] receivedData) {
        switch (receivedData[0]) {
            case VmNetworking.SendBlockChange:
                BlockPos pos = BlockPos.FromBytes(receivedData, 1);
                ushort type = BitConverter.ToUInt16(receivedData, 13);
                ReceiveChange(pos, Block.Create(type, world));
                break;
            case VmNetworking.transmitChunkData:
                ReceiveChunk(receivedData);
                break;
        }
    }

    public void RequestChunk(BlockPos pos)
    {
        if ( debugClient )
            Debug.Log("VmClient.RequestChunk (" + Thread.CurrentThread.ManagedThreadId + "): " + pos);

        byte[] message = new byte[13];
        message[0] = VmNetworking.RequestChunkData;
        pos.ToBytes().CopyTo(message, 1);
        Send(message);
    }

    private void ReceiveChunk(byte[] data)
    {
        BlockPos pos = BlockPos.FromBytes(data, 1);
        Chunk chunk = world.chunks.Get(pos);
        // for now just issue an error if it isn't yet loaded
        if (chunk == null) {
            Debug.LogError("VmClient.ReceiveChunk (" + Thread.CurrentThread.ManagedThreadId + "): "
                + "Could not find chunk for " + pos);
        } else
            chunk.blocks.ReceiveChunkData(data);
    }

    public void BroadcastChange(BlockPos pos, Block block)
    {
        byte[] data = new byte[GetExpectedSize(VmNetworking.SendBlockChange)];

        data[0] = VmNetworking.SendBlockChange;
        pos.ToBytes().CopyTo(data, 1);
        BitConverter.GetBytes(block.type).CopyTo(data, 13);

        Send(data);
    }

    private void ReceiveChange(BlockPos pos, Block block)
    {
        world.blocks.Set(pos, block, updateChunk: true, setBlockModified: false);
    }

    public void Disconnect() {
        if (clientSocket != null)// && clientSocket.Connected)
        {
            //clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            connected = false;
            clientSocket = null;
        }
    }

}
