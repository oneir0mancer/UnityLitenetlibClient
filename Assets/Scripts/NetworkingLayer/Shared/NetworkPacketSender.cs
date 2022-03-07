using LiteNetLib;
using LiteNetLib.Utils;
using NetworkingLayer.Client;

namespace NetworkingLayer.Shared
{
    public class NetworkPacketSender
    {
        public enum TargetGroup : sbyte
        {
            All = -2,
            Others = -1,
        }
        
        private readonly NetSerializer _netSerializer;
        private readonly NetDataWriter _dataWriter = new NetDataWriter();
        
        public NetworkPacketSender()
        {
            _netSerializer = new NetSerializer();
        }

        public NetworkPacketSender(int maxStringLength)
        {
            _netSerializer = new NetSerializer(maxStringLength);
        }
        
        public NetworkPacketSender(NetSerializer serializer)
        {
            _netSerializer = serializer;
        }
        
        //Send custom event package. Structure:
        // * sbyte target : -1 (others), or Id of a Player
        // * byte eventCode : any value from client logic that needs to be <200
        // * byte[] data : custom data that you get from NetDataWriter
        public void SentEvent(NetPeer peer, NetDataWriter customData, byte code, sbyte target, DeliveryMethod deliveryMethod)
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put(code);
            _dataWriter.Put(customData.CopyData());
            peer.Send(_dataWriter, deliveryMethod);
        }
    
        public void SentEvent(NetPeer peer, NetDataWriter customData, byte code, TargetGroup targetGroup, DeliveryMethod deliveryMethod)
        {
            SentEvent(peer, customData, code, (sbyte)targetGroup, deliveryMethod);
        }
        
        public void SentPacket<T>(NetPeer peer, T packet, DedicatedNetCode code, sbyte target, DeliveryMethod deliveryMethod) where T : class, new()
        {
            peer.Send(WriteEventPacket(packet, code, target), deliveryMethod);
        }
        
        public void SentPacket<T>(NetPeer peer, T packet, DedicatedNetCode code, TargetGroup targetGroup, DeliveryMethod deliveryMethod) where T : class, new()
        {
            peer.Send(WriteEventPacket(packet, code, (sbyte)targetGroup), deliveryMethod);
        }
        
        public void SentPacketSerializable<T>(NetPeer peer, T packet, DedicatedNetCode code, sbyte target, DeliveryMethod deliveryMethod) where T : struct, INetSerializable
        {
            peer.Send(WriteEventPacketManual(packet, code, target), deliveryMethod);
        }
        
        public void SentPacketSerializable<T>(NetPeer peer, T packet, DedicatedNetCode code, TargetGroup targetGroup, DeliveryMethod deliveryMethod) where T : struct, INetSerializable
        {
            peer.Send(WriteEventPacketManual(packet, code, (sbyte)targetGroup), deliveryMethod);
        }
    
        //Send dedicated event package. Structure:
        // * sbyte target : -1 (others), or Id of a Player
        // * byte eventCode : one of DedicatedNetCode
        // * byte[] data : data that you get by auto-serializing T class using _netSerializer
        private NetDataWriter WriteEventPacket<T>(T packet, DedicatedNetCode code, sbyte target = -1) where T : class, new()
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put((byte)code);
            _netSerializer.Serialize(_dataWriter, packet);
            return _dataWriter;
        }
        
        private NetDataWriter WriteEventPacketManual<T>(T packet, DedicatedNetCode code, sbyte target = -1) where T : struct, INetSerializable
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put((byte)code);
            packet.Serialize(_dataWriter);
            return _dataWriter;
        }

        //FIXME: This is Client only, TODO: subclass
        //Send stream package. Structure:
        // * sbyte target : ignored by server, always -1
        // * byte eventCode : always DedicatedNetCode.SerializeStream
        // * int entityId : unique id that is allocated in NetworkView and sent with spawn event
        // * byte[] data : data that you get in SendStream implementation
        public void SendStreamPacket(NetPeer peer, ISerializeStream stream, DeliveryMethod deliveryMethod)
        {
            _dataWriter.Reset();
            _dataWriter.Put((sbyte)TargetGroup.Others);
            _dataWriter.Put((byte)DedicatedNetCode.SerializeStream);    //Event code
            _dataWriter.Put(stream.EntityId);

            stream.SendStream(_dataWriter);
        
            peer.Send(_dataWriter, deliveryMethod);
        }
    }
}