//-----------------------------------------------------------------------
// <copyright file="basepacket.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets.Networking
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Packet Magic numbers
    /// </summary>
    public enum EPacketMagicNumbers
    {
        /// <summary>
        /// Number representing the beginning of a packet
        /// </summary>
        PacketBegin = 90495533,

        /// <summary>
        /// Number representing the end of a packet
        /// </summary>
        PacketEnd = 28879791
    }

    /// <summary>
    /// Enum of the included packets.
    /// </summary>
    public enum EBasePackets
    {
        /// <summary>
        /// The set client identifier
        /// </summary>
        SetClientID,

        /// <summary>
        /// The client connected
        /// </summary>
        ClientConnected,

        /// <summary>
        /// The client disconnected
        /// </summary>
        ClientDisconnected,

        /// <summary>
        /// The ping
        /// </summary>
        Ping,

        /// <summary>
        /// Checks if the target is still connected.
        /// </summary>
        Probe,

        /// <summary>
        /// The number of packets
        /// </summary>
        NUM_PACKETS
    }

    /// <summary>
    /// The type of connection for the machine
    /// </summary>
    public enum EConnectionType
    {
        /// <summary>
        /// no type selected
        /// </summary>
        NONE,

        /// <summary>
        /// The server
        /// </summary>
        SERVER,

        /// <summary>
        /// The client
        /// </summary>
        CLIENT
    }

    /// <summary>
    /// Included base packet class to be inherited from.
    /// </summary>
    /// <typeparam name="CustomPacketEnum">The type of the custom packet enum.</typeparam>
    [Serializable]
    public abstract class BasePacket<CustomPacketEnum>
    {
        /// <summary>
        /// The packet target
        /// </summary>
        public EConnectionType PacketTarget = EConnectionType.NONE;
        public int PacketOriginTotalLatency; // Total latency from original packet sender to target, filled out by server
        public int PacketOriginClientID = int.MinValue;

        /// <summary>
        /// Finalizes the packet.
        /// </summary>
        /// <returns>Returns byte array with packet data, ready to send.</returns>
        public byte[] FinalizePacket()
        {
            Debug.Assert(PacketTarget != EConnectionType.NONE, "Packet has to have a Packet Target!");
            Debug.Assert(PacketOriginClientID >= -1, "Packet has to have a PacketOriginClientID!");
            
            int packetID = -1;
            string[] arr = Enum.GetNames(typeof(CustomPacketEnum));
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == GetType().Name.Split('_')[1])
                {
                    packetID = (int)Enum.GetValues(typeof(CustomPacketEnum)).GetValue(i);
                    break;
                }
            }

            byte[] br = Serializer.DataSerializers.ObjectToByteArray(this);
            byte[] outBarr = new byte[3*sizeof(int) + br.Length];
            Array.Copy(BitConverter.GetBytes((int)EPacketMagicNumbers.PacketBegin), outBarr, sizeof(int));
            Array.Copy(BitConverter.GetBytes(packetID), 0, outBarr, sizeof(int), sizeof(int));
            Array.Copy(br, 0, outBarr, 2*sizeof(int), br.Length);
            Array.Copy(BitConverter.GetBytes((int)EPacketMagicNumbers.PacketEnd), 0, outBarr, outBarr.Length - sizeof(int), sizeof(int));

            return outBarr;
        }
    }

    /// <summary>
    /// The Client Connected Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_ClientConnected : BasePacket<EBasePackets>
    {
        /// <summary>
        /// The other client identifier
        /// </summary>
        public int OtherClientID;
    }

    /// <summary>
    /// The Client Disconnected Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_ClientDisconnected : BasePacket<EBasePackets>
    {
        /// <summary>
        /// The other client identifier
        /// </summary>
        public int OtherClientID;
    }

    /// <summary>
    /// The set unique identifier Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_SetClientID : BasePacket<EBasePackets>
    {
        /// <summary>
        /// The identifier
        /// </summary>
        public int Id;

        /// <summary>
        /// The sender identifier
        /// </summary>
        public int SenderID = -1;
    }

    /// <summary>
    /// The Ping Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_Ping : BasePacket<EBasePackets>
    {
        /// <summary>
        /// The time
        /// </summary>
        public int ToServerLatency;
    }

    /// <summary>
    /// The Probe Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_Probe : BasePacket<EBasePackets>
    {
    }
}
