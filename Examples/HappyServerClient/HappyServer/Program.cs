using FastSockets.Networking;
using System;
using System.Collections.Generic;
using System.Net;

namespace HappyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server s = new Server();

            // subscribe to our event
            s.OnMessagePacketReceived += OnMessagePacketReceived;

            s.Start(IPAddress.Any);
            while (s.IsRunning)
            {
                s.Update();
            }
            s.Shutdown();
        }

        static void OnMessagePacketReceived(KeyValuePair<ClientConnection, object> conPkt)
        {
            Console.WriteLine("The Message was received, and the event called!");
        }
    }
}
