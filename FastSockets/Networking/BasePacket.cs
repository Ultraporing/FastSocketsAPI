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
        /// The ping alive
        /// </summary>
        Ping,

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
        /// The login server
        /// </summary>
        LOGIN_SERVER,

        /// <summary>
        /// The supervisor server
        /// </summary>
        SUPERVISOR_SERVER,

        /// <summary>
        /// The sector server
        /// </summary>
        SECTOR_SERVER,

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

        /// <summary>
        /// Finalizes the packet.
        /// </summary>
        /// <returns>Returns byte array with packet data, ready to send.</returns>
        public byte[] FinalizePacket()
        {
            Debug.Assert(PacketTarget != EConnectionType.NONE, "Packet has to have a Packet Target!");

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
            byte[] outBarr = new byte[sizeof(int) + br.Length];
            Array.Copy(BitConverter.GetBytes(packetID), outBarr, sizeof(int));
            Array.Copy(br, 0, outBarr, sizeof(int), br.Length);

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
    /// The Ping alive Packet
    /// </summary>
    // /// <seealso cref="FastSockets.Networking.BasePacket{FastSockets.Networking.EBasePackets}" />
    [Serializable]
    public class PacketDesc_Ping : BasePacket<EBasePackets>
    {
        /// <summary>
        /// The time
        /// </summary>
        public int Time;

        /// <summary>
        /// Is the ping packet returning
        /// </summary>
        public bool IsPong;
    }
}
