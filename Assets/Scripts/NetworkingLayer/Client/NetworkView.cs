using UnityEngine;

namespace NetworkingLayer.Client
{
    //TODO: Photon keeps a list of all ISerializeStreams for a NetworkView, and adds NetworkViews to callback targets
    public class NetworkView : MonoBehaviour
    {
        public event System.Action<Player> OnInitializeEvent;
    
        private static int _peerLastAllocatedId = 10;    //track per peer, TODO fixme: fix spawning player prefab
    
        public int EntityId { get; private set; }
        public bool IsLocal { get; private set;}
        public Player Owner => _owner;
    
        private Player _owner;

        public void Initialize(Player owner, int id, bool local = false)
        {
            _owner = owner;
            EntityId = id;
            IsLocal = local;

            OnInitializeEvent?.Invoke(_owner);
        }

        public static int AllocateEntityId(Player owner)    //TODO fixme
        {
            int id = _peerLastAllocatedId;
            if (id >= 1000)
                throw new System.IndexOutOfRangeException("Ids capped at 1000 per peer");
            id += 1000 * owner.Id;
        
            _peerLastAllocatedId += 1;
            return id;
        }

        protected virtual void OnDestroy()
        {
            if (Client.E_Exists())
                Client.Instance.RemoveNetworkView(this);
        }
    }
}
