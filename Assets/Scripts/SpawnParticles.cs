using NetworkingLayer.Client;
using UnityEngine;
using NetworkView = NetworkingLayer.Client.NetworkView;

public class SpawnParticles : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private NetworkView _particle;

    private void Update()
    {
        if (!Client.Instance.IsConnected) return;

        if (Input.GetMouseButtonDown(1))
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NetworkedPrefabsManager.Instance.NetworkInstantiate(_particle, hit.point, Quaternion.identity);
            }
        }
    }
}
