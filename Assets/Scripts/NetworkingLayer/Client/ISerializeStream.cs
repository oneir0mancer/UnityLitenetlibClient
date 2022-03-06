using LiteNetLib.Utils;

namespace NetworkingLayer.Client
{
    public interface ISerializeStream
    {
        int EntityId { get; }
        bool IsLocal { get; }

        void SendStream(NetDataWriter writer);    //TODO ref ?
        void ReceiveStream(NetDataReader reader); //TODO ref ?
    }
}
