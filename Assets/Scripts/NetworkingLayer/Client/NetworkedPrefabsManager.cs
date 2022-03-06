using System.Collections.Generic;
using LiteNetLib;
using NetworkingLayer.Shared;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NetworkingLayer.Client
{
    [CreateAssetMenu(menuName = "Networked Prefabs")]
    public class NetworkedPrefabsManager : Utils.ScriptableSingleton<NetworkedPrefabsManager>
    {
        public bool UseResourcesFolder => _useRecoursesFolder;
    
        [SerializeField] private List<GameObject> _networkedPrefabs = new List<GameObject>();    //TODO NetView
        [SerializeField] private List<GameObject> _resourcesNetworkedPrefabs = new List<GameObject>();
        [SerializeField] private bool _useRecoursesFolder;
    
        //This scans Resources folder for NetworkViews every time we hit Play
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void PopulateNetworkedPrefabsFromResourcesStatic()
        {
            Instance.PopulateNetworkedPrefabsFromResources();
        }
    
        public void PopulateNetworkedPrefabsFromResources()
        {
#if UNITY_EDITOR
            if (!_useRecoursesFolder) return;
        
            _resourcesNetworkedPrefabs.Clear();
            GameObject[] results = Resources.LoadAll<GameObject>("");
            foreach (var gameObject in results)
            {
                if (gameObject.GetComponent<NetworkView>() != null)
                {
                    if (!_networkedPrefabs.Contains(gameObject))    //if it wasn't added automatically
                        _resourcesNetworkedPrefabs.Add(gameObject);
                }
            }
#endif
        }
    
        public static bool ContainsPrefab(GameObject gameObject)
        {
            //TODO need to have na instance loaded in Editor
            return Instance._networkedPrefabs.Contains(gameObject);
        }
        
        public static void AddNetworkedPrefab(GameObject gameObject)
        {
            if (!Instance._networkedPrefabs.Contains(gameObject))
                Instance._networkedPrefabs.Add(gameObject);
        }

        public void CleanupNetworkedPrefabs()
        {
#if UNITY_EDITOR
            for (int i = Instance._networkedPrefabs.Count - 1; i >= 0; i--)
            {
                if (Instance._networkedPrefabs == null)
                    Instance._networkedPrefabs.RemoveAt(i);
            }
#endif
        }

        //Use to spawn objects from remotes when received corresponding message
        public GameObject NetworkSpawnByIndex(int prefabIndex, int entityId, Player owner, Vector3 position, Quaternion rotation,
            object[] customData = null)
        {
            if (prefabIndex < 0 || prefabIndex > _networkedPrefabs.Count)
                throw new System.ArgumentOutOfRangeException(nameof(prefabIndex));

            var prefab = _networkedPrefabs[prefabIndex];
            
            var obj = Instantiate(prefab, position, rotation);
            obj.GetComponent<NetworkView>().Initialize(owner, entityId, false);

            return obj;
        }

        //Use these methods to spawn objects, synchronized over network
        public GameObject NetworkInstantiate(GameObject obj, Vector3 position, Quaternion rotation, object[] customData = null)
        {
            if (!obj.TryGetComponent(out NetworkView prefabView))
                throw new System.ArgumentException("Networked gameobject should contain NetworkView");

            var view = NetworkInstantiate(prefabView, position, rotation, customData);
            return view == null ? null : view.gameObject;
        }
        
        public NetworkView NetworkInstantiate(NetworkView obj, Vector3 position, Quaternion rotation, object[] customData = null)
        {
            int prefabIndex = _networkedPrefabs.IndexOf(obj.gameObject);
            if (prefabIndex == -1) //TODO resources
            {
                Debug.LogError("Gameobject not found in Resources and wasn't added at runtime");
                return null;
            }

            var player = Client.Instance.LocalPlayer;
            int allocatedEntityId = NetworkView.AllocateEntityId(player);
            SendSpawnEvent(prefabIndex, allocatedEntityId, position, rotation);
                
            var view = Instantiate(obj, position, rotation);
            view.Initialize(player, allocatedEntityId, true);

            return view;
        }

        public void SendSpawnEvent(int prefabIndex, int entityId, Vector3 position, Quaternion rotation, object[] customData = null)
        {
            SpawnObjectPacket pt = new SpawnObjectPacket
            {
                PrefabIndex = prefabIndex,
                EntityId = entityId,
                Position = position,
                RotationAngle = rotation.eulerAngles.y,
            };
            Client.Instance.SentPacket(pt, DedicatedNetCode.SpawnObject, Client.TargetGroup.Others, DeliveryMethod.ReliableOrdered);
        }

        public void NetworkDestroy(GameObject obj)
        {
            if (!obj.TryGetComponent(out NetworkView networkView))
                throw new System.ArgumentException("Networked gameobject should contain NetworkView");

            if (!networkView.IsLocal) return;
            
            DestroySpawnedObjectPacket pt = new DestroySpawnedObjectPacket {EntityId = networkView.EntityId};
            Client.Instance.SentPacket(pt, DedicatedNetCode.DestroySpawnedObject, Client.TargetGroup.Others, DeliveryMethod.ReliableOrdered);
            
            Destroy(obj);
        }
    }
}
