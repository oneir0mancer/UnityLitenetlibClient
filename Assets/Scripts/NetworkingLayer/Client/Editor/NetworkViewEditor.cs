using UnityEditor;
using UnityEngine;

namespace NetworkingLayer.Client
{
    [CustomEditor(typeof(NetworkView))]
    public class NetworkViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NetworkView myTarget = (NetworkView) target;
 
            base.OnInspectorGUI();

            if (myTarget.Owner != null)
            {
                GUILayout.Label($"Owner Id: {myTarget.Owner.Id}");
                GUILayout.Label($"Entity Id: {myTarget.EntityId}");
                GUILayout.Label(myTarget.IsLocal ? "Local" : "Remote");
            }

            if (PrefabUtility.IsPartOfPrefabAsset(myTarget.gameObject))
            {
                if (!NetworkedPrefabsManager.ContainsPrefab(myTarget.gameObject))
                {
                    EditorGUILayout.HelpBox("This prefab isn't in NetworkedPrefabsManager", MessageType.Error);
                    
                    if (GUILayout.Button("Add to Network Prefabs"))
                    {
                        NetworkedPrefabsManager.AddNetworkedPrefab(myTarget.gameObject);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This prefab is in NetworkedPrefabsManager", MessageType.Info);
                }
            }
        }
    }
}
