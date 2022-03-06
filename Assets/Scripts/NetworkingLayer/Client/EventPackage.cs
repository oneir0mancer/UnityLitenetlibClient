using LiteNetLib;

namespace NetworkingLayer.Client
{
    //TODO there is option for timestamping or last exact RTT, mb in OnNetworkLatencyUpdate 
    //https://forum.unity.com/threads/litenetlib-another-reliable-udp-library.414307/page-4#post-3473826
    public class EventPackage
    {
        public int SenderId { get; }
        public byte Code { get; }

        //public int ServerTimestamp { get; }
        public NetPacketReader Data { get; }

        public EventPackage(NetPacketReader reader, int senderId, byte eventCode)
        {
            SenderId = senderId;
            Code = eventCode;
            Data = reader;
        }
    }
}
