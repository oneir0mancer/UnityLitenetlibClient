using LiteNetLib.Utils;
using UnityEngine;

namespace NetworkingLayer.Shared
{
    public static class Extensions
    {      
        public static void Put(this NetDataWriter writer, Vector2 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
        }
        
        public static void Put(this NetDataWriter writer, Vector3 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        public static Vector2 GetVector2(this NetDataReader reader)
        {
            Vector2 v;
            v.x = reader.GetFloat();
            v.y = reader.GetFloat();
            return v;
        }
        
        public static Vector3 GetVector3(this NetDataReader reader)
        {
            Vector3 v;
            v.x = reader.GetFloat();
            v.y = reader.GetFloat();
            v.z = reader.GetFloat();
            return v;
        }
    }
}
