using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

[Serializable]
public class VmNetworking {

    public VmClient client;
    public VmServer server;

    /// <summary> True if this world is hosted by the player, not someone else </summary>
    public bool isServer = true;
    public bool allowConnections = false;

    public void StartConnections(World world)
    {
        if (!isServer)
        {
            client = new VmClient(world);
        }
        else if (allowConnections)
        {
            server = new VmServer(world);
        }
    }

    public IPAddress serverIP = new IPAddress(new byte[] { 127, 0, 0, 1 });
    public int serverPort = 11000;

    public const int bufferLength = 1024;

    public const byte SendBlockChange = 1;
    public const byte RequestChunkData = 2;
    public const byte transmitChunkData = 3;

}
