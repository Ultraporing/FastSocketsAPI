using FastSockets.Networking;
using HappyLib.Packets;
using System;
using System.Collections.Generic;

namespace HappyServer
{
    public class Server : BaseServer<EHappyPackets, Server>
    {
        // Create event to be used somewhere else (optitional)
        public event OnPacketReceivedCallback OnMessagePacketReceived;

        // if we pass an empty string as logfile, we dont use logfiles
        // there is also a version without specifying a config file. If that is used you have to load the configs yourself!
        // ----
        // Example (loading config from string):
        // string cfgString = "PORT=1234;NAME=Herpderp;";
        // LoadConfigFromString(cfgString);
        // SetPort();
        // ---
        // Example (loading config from file):
        // string cfgFileName = "myconfig.cfg";
        // LoadConfigFromFile(cfgFileName);
        // SetPort();
        // ---
        public Server() :  base("HappyServer.cfg", "HappyServerLog", EConnectionType.SECTOR_SERVER, 1000, 5000)
        {
            OnClientAccepted += Server_OnClientAccepted;
            OnClientDisconnected += Server_OnClientDisconnected;
        }

        public override void Update()
        {
            base.Update();
            // do something
        }

        protected override string GetConsoleWindowTitle()
        {
            return "Happy Server - " + base.GetConsoleWindowTitle();
        }

        private void Server_OnClientAccepted(int clientID)
        {
            Console.WriteLine("Server has accepted a new Client with the ID: " + clientID);
        }

        private void Server_OnClientDisconnected(int clientID)
        {
            Console.WriteLine("Server has disconnected the Client with the ID: " + clientID);
        }

        protected bool ReceivedPacket_Message(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_Message packet = (PacketDesc_Message)conPkt.Value;

            Console.WriteLine("Recieved Message Packet! Client ID: " + conPkt.Key.ThisID + ", Data: " + packet.Message);

            PacketDesc_Message msg = new PacketDesc_Message();
            msg.PacketTarget = EConnectionType.SECTOR_SERVER;
            msg.Message = "I like Turtles!";
            SendPacketToClient(msg, conPkt.Key.ThisID);

            if (OnMessagePacketReceived != null)
            {
                OnMessagePacketReceived(conPkt);
            }

            return true;
        }
    }
}
