using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;
using NetCode = System.Byte;    //TODO fixme: alias for `byte`

namespace NetworkingLayer.Shared
{
    public class NetworkPacketProcessor
    {
        private delegate void SubscribeDelegate(NetDataReader reader, object userData);

        private SubscribeDelegate _defaultCallback;
        private readonly Dictionary<NetCode, SubscribeDelegate> _callbacks = new Dictionary<NetCode, SubscribeDelegate>();
        private readonly NetSerializer _netSerializer;
        //private readonly NetDataWriter _netDataWriter = new NetDataWriter();
        
        public NetworkPacketProcessor()
        {
            _netSerializer = new NetSerializer();
        }

        public NetworkPacketProcessor(int maxStringLength)
        {
            _netSerializer = new NetSerializer(maxStringLength);
        }
        
        public NetworkPacketProcessor(NetSerializer serializer)
        {
            _netSerializer = serializer;
        }
        
        //Use this to add INetSerializable Client packets ?
        public void RegisterNestedType<T>() where T : struct, INetSerializable
        {
            _netSerializer.RegisterNestedType<T>();
        }

        private SubscribeDelegate GetCallbackFromData(NetDataReader reader)
        {
            NetCode packetNetCode = reader.GetByte();

            if (!_callbacks.TryGetValue(packetNetCode, out SubscribeDelegate action))
            {
                if (_defaultCallback == null) 
                    throw new ParseException($"Callback for eventCode={packetNetCode} is not set");
                return _defaultCallback;
            }
            return action;
        }
        
        public void ReadAllPackets(NetDataReader reader, object userData = null)
        {
            while (reader.AvailableBytes > 0)
                ReadPacket(reader, userData);
        }
        
        public void ReadPacket(NetDataReader reader, object userData = null)
        {
            GetCallbackFromData(reader).Invoke(reader, userData);
        }
        
        public void Subscribe(NetCode packetNetCode, Action<NetDataReader> onReceive)
        {
            if (_callbacks.ContainsKey(packetNetCode))
                throw new ArgumentException($"Rewriting existing callback for eventCode={packetNetCode}");
            
            _callbacks[packetNetCode] = (reader, userData) =>
            {
                onReceive(reader);
            };
        }
        
        public void SubscribeDefault<TUserData>(Action<NetDataReader, TUserData> onReceive)
        {
            if (_defaultCallback != null)
                throw new ArgumentException($"Rewriting default callback for custom events");
            
            _defaultCallback = (reader, userData) =>
            {
                onReceive(reader, (TUserData)userData);
            };
        }
        
        public void Subscribe<T>(NetCode packetNetCode, Action<T> onReceive) where T : class, new()
        {
            if (_callbacks.ContainsKey(packetNetCode))
                throw new ArgumentException($"Rewriting existing callback for eventCode={packetNetCode}");
            
            _netSerializer.Register<T>();
            var reference = new T();
            _callbacks[packetNetCode] = (reader, userData) =>
            {
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference);
            };
        }
        
        public void Subscribe<T, TUserData>(NetCode packetNetCode, Action<T, TUserData> onReceive) where T : class, new()
        {
            if (_callbacks.ContainsKey(packetNetCode))
                throw new ArgumentException($"Rewriting existing callback for eventCode={packetNetCode}");
            
            _netSerializer.Register<T>();
            var reference = new T();
            _callbacks[packetNetCode] = (reader, userData) =>
            {
                _netSerializer.Deserialize(reader, reference);
                onReceive(reference, (TUserData)userData);
            };
        }
        
        public void SubscribeNetSerializable<T>(NetCode packetNetCode,
            Action<T> onReceive) where T : INetSerializable, new()
        {
            if (_callbacks.ContainsKey(packetNetCode))
                throw new ArgumentException($"Rewriting existing callback for eventCode={packetNetCode}");
            
            var reference = new T();
            _callbacks[packetNetCode] = (reader, userData) =>
            {
                reference.Deserialize(reader);
                onReceive(reference);
            };
        }
        
        public void SubscribeNetSerializable<T, TUserData>(NetCode packetNetCode,
            Action<T, TUserData> onReceive) where T : INetSerializable, new()
        {
            if (_callbacks.ContainsKey(packetNetCode))
                throw new ArgumentException($"Rewriting existing callback for eventCode={packetNetCode}");
            
            var reference = new T();
            _callbacks[packetNetCode] = (reader, userData) =>
            {
                reference.Deserialize(reader);
                onReceive(reference, (TUserData)userData);
            };
        }
        
        public bool RemoveSubscription(NetCode packetNetCode)
        {
            return _callbacks.Remove(packetNetCode);
        }
    }
}