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
        //private const string ConnectionHost = "194.58.100.49";
        private const string ConnectionHost = "localhost";
        private const int ConnectionPort = 9050;
        private const string ConnectionKey = "SomeConnectionKey";

        public Player LocalPlayer => _localPlayer;

        public bool IsConnected => _isConnected;

        public string NickName;    //TODO fixme
    
        [SerializeField] private NetworkView _playerPrefab;
        [SerializeField] private InputField _nameInput;
    
        private List<IOnEventCallback> _callbackTargets = new List<IOnEventCallback>();
        private List<ISerializeStream> _streamTargets = new List<ISerializeStream>();    //TODO track ISerializeStreams in NetworkView
        private List<NetworkView> _networkObjects = new List<NetworkView>();
    
        private readonly NetSerializer _netSerializer = new NetSerializer();
        private NetworkPacketProcessor _netProcessor;
        private NetworkPacketSender _netSender;
    
        private NetManager _netManager;
        private NetPeer _server;
        private Player _localPlayer;
        private List<Player> _otherPlayers = new List<Player>();
        private bool _isConnected;

        private void Awake()
        {
            _netSender = new NetworkPacketSender(_netSerializer);
            _netProcessor = new NetworkPacketProcessor(_netSerializer);
            InitializePacketCallbacks();
            
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

        private void InitializePacketCallbacks()
        {
            //TODO do I need to RegisterNestedType<SpawnObjectPacket> ?
            
            _netProcessor.Subscribe<JoinAcceptPacket>((byte)DedicatedNetCode.Join, OnJoinAccepted);
            _netProcessor.Subscribe<PlayerJoinedPacket>((byte)DedicatedNetCode.PlayerJoined, OnPlayerJoined);
            _netProcessor.Subscribe<PlayerLeftPacket>((byte)DedicatedNetCode.PlayerLeft, OnPlayerLeft);
            _netProcessor.SubscribeNetSerializable<SpawnObjectPacket>((byte)DedicatedNetCode.SpawnObject, OnSpawnObject);
            _netProcessor.Subscribe<DestroySpawnedObjectPacket>((byte)DedicatedNetCode.DestroySpawnedObject, OnDestroySpawnedObject);
            _netProcessor.Subscribe((byte) DedicatedNetCode.SerializeStream, HandleSerializeStream);
            
            _netProcessor.SubscribeDefault<sbyte>(OnCustomEventPackage);    //sbyte sender
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
                    _netSender.SendStreamPacket(_server, stream, DeliveryMethod.Unreliable);
                }
            
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        public void SentEvent(NetDataWriter customData, byte code, sbyte target, DeliveryMethod deliveryMethod)
        {
            _netSender.SentEvent(_server, customData, code, target, deliveryMethod);
        }
    
        public void SentEvent(NetDataWriter customData, byte code, NetworkPacketSender.TargetGroup targetGroup, DeliveryMethod deliveryMethod)
        {
            _netSender.SentEvent(_server, customData, code, targetGroup, deliveryMethod);
        }
        
        //Need to call this overload for packets that are auto-serializable classes
        public void SentPacket<T>(T packet, DedicatedNetCode code, sbyte target, DeliveryMethod deliveryMethod) where T : class, new()
        {
            _netSender.SentPacket(_server, packet, code, target, deliveryMethod);
        }
        
        //Need to call this overload for packets that are auto-serializable classes
        public void SentPacket<T>(T packet, DedicatedNetCode code, NetworkPacketSender.TargetGroup targetGroup, DeliveryMethod deliveryMethod) where T : class, new()
        {
            _netSender.SentPacket(_server, packet, code, targetGroup, deliveryMethod);
        }
        
        //Need to call this overload for packets that are INetSerializable structs
        public void SentPacketSerializable<T>(T packet, DedicatedNetCode code, sbyte target, DeliveryMethod deliveryMethod) where T : struct, INetSerializable
        {
            _netSender.SentPacketSerializable(_server, packet, code, target, deliveryMethod);
        }
        
        //Need to call this overload for packets that are INetSerializable structs
        public void SentPacketSerializable<T>(T packet, DedicatedNetCode code, NetworkPacketSender.TargetGroup targetGroup, DeliveryMethod deliveryMethod) where T : struct, INetSerializable
        {
            _netSender.SentPacketSerializable(_server, packet, code, targetGroup, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[CLIENT] We connected to " + peer.EndPoint);

            _server = peer;
            JoinReceivedPacket jr = new JoinReceivedPacket {UserName = NickName};
            SentPacket(jr, DedicatedNetCode.Join, NetworkPacketSender.TargetGroup.Others, DeliveryMethod.ReliableOrdered);
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
            sbyte sender = reader.GetSByte();
            _netProcessor.ReadAllPackets(reader, sender);
        }

        private void OnCustomEventPackage(NetDataReader reader, sbyte sender)
        {
            EventPackage eventPackage = new EventPackage(reader, sender);
            foreach (var target in _callbackTargets)
            {
                target.OnEvent(eventPackage);
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

        private void HandleSerializeStream(NetDataReader reader)
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
