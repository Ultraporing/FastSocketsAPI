using FastSockets.Networking;
using HappyLib.Packets;
using System;
using System.Collections.Generic;

namespace HappyClient
{
    public class Client : BaseClient<EHappyPackets, Client>
    {
        // Create event to be used somewhere else (optitional)
        public event OnPacketReceivedCallback OnMessagePacketReceived;

        // if we pass an empty string as logfile, we dont use logfiles
        public Client() : base("ClientLog", 5000)
        {
            OnClientConnectedReceived += Client_OnClientConnectedReceived;
            OnClientDisconnectedReceived += Client_OnClientDisconnectedReceived;
            OnConnectionSuccess += Client_OnConnectionSuccess;
            OnConnectionFailed += Client_OnConnectionFailed;
            OnSetClientIDReceived += Client_OnSetClientIDReceived;
            OnDisconnected += Client_OnDisconnected;
        }

        public override void Update()
        {
            base.Update();
            // do something
        }

        protected override string GetConsoleWindowTitle()
        {
            return "Happy Client - " + base.GetConsoleWindowTitle();
        }

        protected bool ReceivedPacket_Message(KeyValuePair<ClientConnection, object> conPkt)
        {
            PacketDesc_Message packet = (PacketDesc_Message)conPkt.Value;

            Console.WriteLine("Recieved Message Packet! Client ID: " + conPkt.Key.ThisID + ", Data: " + packet.Message + ", Ping: " + packet.PacketOriginTotalLatency);

            if (OnMessagePacketReceived != null)
            {
                OnMessagePacketReceived(conPkt);
            }

            return true;
        }

        private void Client_OnClientConnectedReceived(KeyValuePair<ClientConnection, object> pkt)
        {
            Console.WriteLine("Client has received the Client Connected Packet. Sender: " + pkt.Key.ThisID + ".");
        }

        private void Client_OnClientDisconnectedReceived(KeyValuePair<ClientConnection, object> pkt)
        {
            Console.WriteLine("Client has received the Client Disconnected Packet. Sender: " + pkt.Key.ThisID + ".");
        }

        private void Client_OnSetClientIDReceived(KeyValuePair<ClientConnection, object> pkt)
        {
            Console.WriteLine("Client has received the SetClientID Packet. Sender: " + pkt.Key.ThisID + ". Data: " + ((PacketDesc_SetClientID)pkt.Value).Id);
        }

        private void Client_OnConnectionSuccess()
        {
            Console.WriteLine("Client has Successfully connected the Server.");
        }

        private void Client_OnConnectionFailed()
        {
            Console.WriteLine("Client has Failed to connect to the Server.");
        }

        private void Client_OnDisconnected()
        {
            Console.WriteLine("Client has Disconnected.");
        }

    }
}
