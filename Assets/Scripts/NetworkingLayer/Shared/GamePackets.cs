using LiteNetLib.Utils;

namespace NetworkingLayer.Shared
{
    //Config that tells where DedicatedNetCodes start and where ServerNetCodes start
    public static class GamePackets
    {
        public const int DedicatedNetCodes = 200;
        public const int ServerNetCodes = 215;
    }

    //https://github.com/RevenantX/NetGameExample/blob/master/Assets/Code/Shared/GamePackets.cs
    //TODO simplified version of https://github.com/RevenantX/NetGameExample/blob/master/Assets/Plugins/LiteNetLib/Utils/NetPacketProcessor.cs
    public enum DedicatedNetCode : byte
    {
        SpawnObject = GamePackets.DedicatedNetCodes,
        SerializeStream,
        DestroySpawnedObject,
        //LoadScene

        Join = GamePackets.ServerNetCodes,    //JoinReceived for server, JoinAccepted for client
        PlayerJoined,  //For other peers
        PlayerLeft,
        //PlayerReady
    }

    #region Autoserialized packets

    public class JoinReceivedPacket
    {
        public string UserName { get; set; }
    }
    
    public class JoinAcceptPacket
    {
        public byte Id { get; set; }
    }
    
    public class PlayerJoinedPacket
    {
        public byte Id { get; set; }
        public string UserName { get; set; }
        public bool NewPlayer{ get; set; }
    }
    
    public class PlayerLeftPacket
    {
        public byte Id { get; set; }
    }
    
    #endregion

    #region Manually serialized packets

    //TODO This can be just forwarded to players, we don't care to deserialize it
    public class PlayerCustomPropertiesChangedPacket : INetSerializable
    {
        public byte Id;
        //Hashtable changed data
        
        public void Serialize(NetDataWriter writer)
        {
            //writer.Put(PlayerId);    string key
            //writer.Put(Position);    object value
        }

        public void Deserialize(NetDataReader reader)
        {
            //while reader can read ...
        }
    }
    
    #endregion
}