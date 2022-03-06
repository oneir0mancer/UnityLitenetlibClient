using UnityEditor;
using UnityEngine;

namespace NetworkingLayer.Client
{
    [CustomEditor(typeof(NetworkedPrefabsManager))]
    public class NetworkedPrefabsManagerEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            NetworkedPrefabsManager myTarget = (NetworkedPrefabsManager) target;
 
            if (!NetworkedPrefabsManager.E_Exists())
            {
                EditorGUILayout.HelpBox("No singleton instance loaded", MessageType.Warning);
            }
            else
            {
                if (NetworkedPrefabsManager.Instance == myTarget)
                {
                    EditorGUILayout.HelpBox("This is singleton instance", MessageType.Info);
                }
            }
            
            base.OnInspectorGUI();

            if (myTarget.UseResourcesFolder)
            {
                if (GUILayout.Button("Repopulate from Resources"))
                {
                    myTarget.PopulateNetworkedPrefabsFromResources();
                }
                
                if (GUILayout.Button("Clean-up"))
                {
                    myTarget.CleanupNetworkedPrefabs();
                }
            }
        }
    }
}
