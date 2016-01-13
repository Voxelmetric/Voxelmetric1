using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class VmClient
{
    protected World world;
    int id;
    private Socket clientSocket;
    private byte[] buffer = new byte[VmNetworking.bufferLength];

    public bool connected;

    // The response from the remote device.
    private static string response = string.Empty;

    public VmClient(World world)
    {
        this.world = world;
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.BeginConnect(new IPEndPoint(world.networking.serverIP, world.networking.serverPort),
            new AsyncCallback(OnConnect), null);
    }
    
    private void OnConnect(IAsyncResult ar)
    {
        try
        {
            clientSocket.EndConnect(ar);
            connected = true;
            clientSocket.BeginReceive(buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromServer), null);
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
            int received = clientSocket.EndReceive(ar);

            if (received == 0)
            {
                Debug.Log("disconnected from server");
                clientSocket.Close();
                connected = false;
                return;
            }

            RouteMessageToFunction(buffer);

            clientSocket.BeginReceive(buffer, 0, VmNetworking.bufferLength, SocketFlags.None, new AsyncCallback(OnReceiveFromServer), null);
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
        }
        catch (Exception ex) 
        {
            Debug.LogError(ex);
            connected = false;
            clientSocket.Close();
        }
    }

    public virtual void RouteMessageToFunction(byte[] receivedData)
    {

        switch (receivedData[0]) {
            case VmNetworking.SendBlockChange:
                BlockPos pos = BlockPos.FromBytes(receivedData, 1);
                ReceiveChange(pos, Block.New(BitConverter.ToUInt16(receivedData, 13), world));
                break;
            case VmNetworking.transmitChunkData:
                ReceiveChunk(buffer);
                break;
        }
    }

    internal void Disconnect()
    {
        if (clientSocket != null && clientSocket.Connected)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }

    public void RequestChunk(BlockPos pos)
    {
        byte[] message = new byte[13];
        message[0] = VmNetworking.RequestChunkData;
        pos.ToBytes().CopyTo(message, 1);
        Send(message);
    }

    public void ReceiveChunk(byte[] data)
    {
        BlockPos pos = BlockPos.FromBytes(data, 1);
        world.chunks.Get(pos).blocks.ReceiveChunkData(data);
    }

    public void BroadcastChange(BlockPos pos, Block block)
    {
        byte[] data = new byte[15];

        data[0] = VmNetworking.SendBlockChange;
        pos.ToBytes().CopyTo(data, 1);
        BitConverter.GetBytes(block.type).CopyTo(data, 13);

        Send(data);
    }

    public void ReceiveChange(BlockPos pos, Block block)
    {
        world.blocks.Set(pos, block, updateChunk: true, setBlockModified: false);
    }
}
