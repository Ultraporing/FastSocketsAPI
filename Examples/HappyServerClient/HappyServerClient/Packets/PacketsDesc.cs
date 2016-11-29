using FastSockets.Networking;
using System;

namespace HappyLib.Packets
{
    public enum EHappyPackets
    {
        Message = EBasePackets.NUM_PACKETS,
        NUM_PACKETS
    }

    [Serializable]
    public class PacketDesc_Message : BasePacket<EHappyPackets>
    {
        public string Message;
    }
}
