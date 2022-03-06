using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using NetworkingLayer.Shared;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Random = UnityEngine.Random;

namespace NetworkingLayer.Client
{
    //TODO make this a static class
    public class Client : Singleton<Client>, INetEventListener
    {
        private const string ConnectionHost = "194.58.100.49";
        private const int ConnectionPort = 9050;
        private const string ConnectionKey = "SomeConnectionKey";

        public enum TargetGroup : sbyte
        {
            All = -2,
            Others = -1,
        }

        public Player LocalPlayer => _localPlayer;

        public bool IsConnected => _isConnected;

        public string NickName;    //TODO fixme
    
        [SerializeField] private NetworkView _playerPrefab;
        [SerializeField] private InputField _nameInput;
    
        private List<IOnEventCallback> _callbackTargets = new List<IOnEventCallback>();
        private List<ISerializeStream> _streamTargets = new List<ISerializeStream>();
        private List<NetworkView> _networkObjects = new List<NetworkView>();
    
        private NetManager _netManager;
        private readonly NetDataWriter _dataWriter = new NetDataWriter();
        private readonly NetSerializer _netSerializer = new NetSerializer();
    
        private NetPeer _server;
        private Player _localPlayer;
        private List<Player> _otherPlayers = new List<Player>();
        private bool _isConnected;

        private void Awake()
        {
            _netManager = new NetManager(this)
            {
                UnconnectedMessagesEnabled = true, 
                UpdateTime = 50,
                AutoRecycle = true,
            };

            _netManager.Start();
        }

        public void ConnectToServer()
        {
            if (_isConnected) return;

            NickName = _nameInput.text;
        
            _netManager.Connect(ConnectionHost, ConnectionPort, ConnectionKey);
            Debug.Log("Connecting...");
            _isConnected = true;

            StartCoroutine(SerializeStreamsCoroutine());
        }

        private void Update()
        {
            if (_isConnected)
                _netManager.PollEvents();
        }

        private void OnDestroy()
        {
            _netManager?.Stop();
        }

        public static void AddCallbackTarget(IOnEventCallback target)
        {
            Instance._callbackTargets.Add(target);
        }
    
        public static void RemoveCallbackTarget(IOnEventCallback target)
        {
            Instance._callbackTargets.Remove(target);
        }
    
        public static void AddCallbackTarget(ISerializeStream target)
        {
            Instance._streamTargets.Add(target);
        }
    
        public static void RemoveCallbackTarget(ISerializeStream target)
        {
            Instance._streamTargets.Remove(target);
        }

        public bool RemoveNetworkView(NetworkView view)
        {
            return _networkObjects.Remove(view);
        }
    
        //TODO use Task/Thread
        private IEnumerator SerializeStreamsCoroutine()
        {
            bool isConnected = true;
            while (isConnected)
            {
                foreach (var stream in _streamTargets)
                {
                    if (!stream.IsLocal) continue;
                    WriteStreamPacket(stream, DeliveryMethod.Unreliable);
                }
            
                yield return new WaitForSeconds(0.05f);
            }
        }
    
        //Send custom event package. Structure:
        // * sbyte target : -1 (others), or Id of a Player
        // * byte eventCode : any value from client logic that needs to be <200
        // * byte[] data : custom data that you get from NetDataWriter
        //TODO not use NetDataWriter
        public void SentEvent(NetDataWriter customData, byte code, sbyte target, DeliveryMethod deliveryMethod)
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put(code);
            //TODO Id?
            _dataWriter.Put(customData.CopyData());
            _server.Send(_dataWriter, deliveryMethod);
        }
    
        public void SentEvent(NetDataWriter customData, byte code, TargetGroup targetGroup, DeliveryMethod deliveryMethod)
        {
            SentEvent(customData, code, (sbyte) targetGroup, deliveryMethod);
        }
        
        public void SentPacket<T>(T packet, DedicatedNetCode code, sbyte target, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (packet is INetSerializable netSerializable)
                _server.Send(WriteEventPacketManual(netSerializable, code, target), deliveryMethod);
            else
                _server.Send(WriteEventPacket(packet, code, target), deliveryMethod);
        }
        
        public void SentPacket<T>(T packet, DedicatedNetCode code, TargetGroup targetGroup, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (packet is INetSerializable netSerializable)
                _server.Send(WriteEventPacketManual(netSerializable, code, (sbyte)targetGroup), deliveryMethod);
            else
                _server.Send(WriteEventPacket(packet, code, (sbyte)targetGroup), deliveryMethod);
        }
    
        //Send dedicated event package. Structure:
        // * sbyte target : -1 (others), or Id of a Player
        // * byte eventCode : one of DedicatedNetCode
        // * byte[] data : data that you get by auto-serializing T class using _netSerializer
        //TODO shared with server, so maybe not here
        private NetDataWriter WriteEventPacket<T>(T packet, DedicatedNetCode code, sbyte target = -1) where T : class, new()
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put((byte)code);
            //TODO Id?
            _netSerializer.Serialize(_dataWriter, packet);
            return _dataWriter;
        }
        
        private NetDataWriter WriteEventPacketManual<T>(T packet, DedicatedNetCode code, sbyte target = -1) where T : INetSerializable
        {
            _dataWriter.Reset();
            _dataWriter.Put(target);
            _dataWriter.Put((byte)code);
            packet.Serialize(_dataWriter);
            return _dataWriter;
        }

        //Send stream package. Structure:
        // * sbyte target : ignored by server, always -1
        // * byte eventCode : always DedicatedNetCode.SerializeStream
        // * int entityId : unique id that is allocated in NetworkView and sent with spawn event
        // * byte[] data : data that you get in SendStream implementation
        public void WriteStreamPacket(ISerializeStream stream, DeliveryMethod deliveryMethod)
        {
            _dataWriter.Reset();
            _dataWriter.Put((sbyte)-1);    //Target
            _dataWriter.Put((byte)DedicatedNetCode.SerializeStream);    //Event code
            _dataWriter.Put(stream.EntityId);

            stream.SendStream(_dataWriter);
        
            _server.Send(_dataWriter, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[CLIENT] We connected to " + peer.EndPoint);

            _server = peer;
            JoinReceivedPacket jr = new JoinReceivedPacket {UserName = NickName};
            _server.Send(WriteEventPacket(jr, DedicatedNetCode.Join), DeliveryMethod.ReliableOrdered);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[CLIENT] We disconnected because " + disconnectInfo.Reason);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[CLIENT] We received error " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            //We get all the network msgs here. Then we turn packages into specific events with args
            // and go through every script with OnEvent (need to manage a list)
        
            sbyte sender = reader.GetSByte();
            byte packetNetCode = reader.GetByte();

            if (packetNetCode >= GamePackets.DedicatedNetCodes)
            {
                DedicatedNetCode dedicatedNetCode = (DedicatedNetCode) packetNetCode;
                switch (dedicatedNetCode)
                {
                    case DedicatedNetCode.Join:
                        OnJoinAccepted(_netSerializer.Deserialize<JoinAcceptPacket>(reader));
                        break;
                    case DedicatedNetCode.PlayerJoined:
                        OnPlayerJoined(_netSerializer.Deserialize<PlayerJoinedPacket>(reader));
                        break;
                    case DedicatedNetCode.PlayerLeft:
                        OnPlayerLeft(_netSerializer.Deserialize<PlayerLeftPacket>(reader));
                        break;
                    case DedicatedNetCode.SerializeStream:
                        HandleSerializeStream(reader);
                        break;
                    case DedicatedNetCode.SpawnObject:
                        SpawnObjectPacket spawnPt = new SpawnObjectPacket();
                        spawnPt.Deserialize(reader);
                        OnSpawnObject(spawnPt);
                        break;
                    case DedicatedNetCode.DestroySpawnedObject:
                        OnDestroySpawnedObject(_netSerializer.Deserialize<DestroySpawnedObjectPacket>(reader));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            { 
                EventPackage eventPackage = new EventPackage(reader, sender, packetNetCode);
                foreach (var target in _callbackTargets)
                {
                    target.OnEvent(eventPackage);
                }
            }
        }

        private void OnJoinAccepted(JoinAcceptPacket acceptPacket)
        {
            _localPlayer = new Player(acceptPacket.Id, NickName);

            Vector3 pos = new Vector3(Random.Range(0, 5), 0, Random.Range(0, 5));
            var netView = Instantiate(_playerPrefab, pos, Quaternion.identity);
            netView.Initialize(_localPlayer, acceptPacket.Id, true);
        
            _networkObjects.Add(netView);
        }
    
        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            var player = new Player(packet.Id, packet.UserName);
            _otherPlayers.Add(player);
        
            var netView = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
            netView.Initialize(player, packet.Id);
        
            _networkObjects.Add(netView);
        }

        private void OnPlayerLeft(PlayerLeftPacket packet)
        {
            //TODO fixme
            var playerObject = _networkObjects.Find(x => x.EntityId == packet.Id);
            if (playerObject != null)
            {
                _networkObjects.Remove(playerObject);
                Destroy(playerObject.gameObject);
            }
        }
        
        private void OnSpawnObject(SpawnObjectPacket objectPacket)
        {
            var player = _otherPlayers.Find(x => x.Id == objectPacket.PlayerId);
            var obj = NetworkedPrefabsManager.Instance.NetworkSpawnByIndex(objectPacket.PrefabIndex, objectPacket.EntityId, player,
                objectPacket.Position, objectPacket.Rotation);
            
            //TODO fixme
            _networkObjects.Add(obj.GetComponent<NetworkView>());
        }

        private void OnDestroySpawnedObject(DestroySpawnedObjectPacket packet)
        {
            var obj = _networkObjects.Find(x => x.EntityId == packet.EntityId);
            Destroy(obj.gameObject);
        }

        private void HandleSerializeStream(NetPacketReader reader)
        {
            int entityId = reader.GetInt();
        
            foreach (var stream in _streamTargets)
            {
                if (stream.EntityId != entityId) continue;
            
                if (stream.IsLocal)
                    Debug.LogError("Got stream serialization for local object");
            
                stream.ReceiveStream(reader);
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        { }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        { }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        { }
    }
}
