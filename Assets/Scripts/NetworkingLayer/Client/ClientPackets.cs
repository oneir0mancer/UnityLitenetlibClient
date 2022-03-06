using LiteNetLib.Utils;
using NetworkingLayer.Shared;
using UnityEngine;

namespace NetworkingLayer.Client
{
    public class SpawnObjectPacket : INetSerializable
    {
        public int PrefabIndex;
        public int EntityId;
        public Vector3 Position;
        public float RotationAngle;
        //public object[] CustomData;
        
        public int PlayerId => EntityId / 100;    //TODO fixme
        public Quaternion Rotation => Quaternion.Euler(0, RotationAngle, 0);
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PrefabIndex);
            writer.Put(EntityId);
            writer.Put(Position);
            writer.Put(RotationAngle);
        }

        public void Deserialize(NetDataReader reader)
        {
            PrefabIndex = reader.GetInt();
            EntityId = reader.GetInt();
            Position = reader.GetVector3();
            RotationAngle = reader.GetFloat();
        }
    }
    
    public class DestroySpawnedObjectPacket
    {
        public int EntityId { get; set; }
    }
}