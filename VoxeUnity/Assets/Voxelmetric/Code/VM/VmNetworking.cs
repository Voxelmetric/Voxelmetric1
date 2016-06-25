using System;
using System.Net;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.VM
{
    [Serializable]
    public class VmNetworking {

        public VmClient client;
        public VmServer server;

        //public string serverIPAdress;
        public IPAddress serverIP;
        //public int serverPort = 11000;

        public const int bufferLength = 1024;

        public const byte SendBlockChange = 1;
        public const byte RequestChunkData = 2;
        public const byte transmitChunkData = 3;

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
}
