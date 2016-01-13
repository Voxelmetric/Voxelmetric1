using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class VmNetworking {

    public IPAddress serverIP = new IPAddress(new byte[] { 192, 168, 0, 2 });
    public int serverPort = 11000;

    public const int bufferLength = 1024;

    public const byte SendBlockChange = 1;
    public const byte RequestChunkData = 2;
    public const byte transmitChunkData = 3;

}
