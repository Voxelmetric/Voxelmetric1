using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

[Serializable]
public class VmNetworking {

    public VmClient client;
    public VmServer server;

    //public string serverIPAdress;
    public IPAddress serverIP;
    //public int serverPort = 11000;

    public const int bufferLength = 1024;

    public class SendBlockChange {
        public const byte ID = 1;

        public const int IdxBlockPos = 1;
        public const int IdxBlockType = IdxBlockPos + 12;

        public const int Size = IdxBlockType + 2;
    }
    public class RequestChunkData {
        public const byte ID = 2;

        public const int IdxBlockPos = 1;

        public const int Size = IdxBlockPos + 12;
    }
    public class TransmitChunkData {
        public const byte ID = 3;

        public const int IdxSize = 1;
        public const int IdxChunkPos = IdxSize + 4;
        public const int IdxDataOffset = IdxChunkPos + 12;
        public const int IdxDataLength = IdxDataOffset + 4;
        public const int IdxData = IdxDataLength + 4;

        public const int HeaderSize = IdxData;

        public const bool UseVariableMessageLength = true;
    }

    /// <summary> True if this world is hosted by the player, not someone else </summary>
    public bool isServer = true;
    public bool allowConnections = false;

    public void StartConnections(World world)
    {
        if (!isServer)
        {
            //string[] ipSplit = serverIPAdress.Split('.');
            //if (ipSplit.Length < 4)
            //{
            //    Debug.LogError("ip must be 4 period separated integers");
            //    return;
            //}

            //byte[] ipComponents = new byte[4];
            //for (int i = 0; i < 4; i++)
            //{
            //    ipComponents[i] = byte.Parse(ipSplit[i]);
            //}
            //serverIP = new IPAddress(ipComponents);
            client = new VmClient(world, serverIP);
        }
        else if (allowConnections)
        {
            server = new VmServer(world);
        }
    }

    public void EndConnections() {
        if (client != null) {
            client.Disconnect();
            client = null;
        }
        if (server != null) {
            server.Disconnect();
            server = null;
        }
    }
}
