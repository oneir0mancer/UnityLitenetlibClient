using NetworkingLayer.Client;
using UnityEngine;
using NetworkView = NetworkingLayer.Client.NetworkView;

public class OwnerIdToColor : MonoBehaviour
{
    [SerializeField] private NetworkView _view;
    [SerializeField] private MeshRenderer _mesh;
    [SerializeField, Min(0)] private int _maxColors = 5;
    
    private void Awake()
    {
        _view.OnInitializeEvent += IdToColor;
    }

    private void IdToColor(Player player)
    {
        float randomHue = player.Id / (float)_maxColors;
        _mesh.material.color = Color.HSVToRGB(randomHue, 1f, 1f);
    }
}
