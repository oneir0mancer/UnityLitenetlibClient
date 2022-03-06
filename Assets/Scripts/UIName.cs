using NetworkingLayer.Client;
using TMPro;
using UnityEngine;
using NetworkView = NetworkingLayer.Client.NetworkView;

public class UIName : MonoBehaviour
{
    [SerializeField] private NetworkView _view;
    [SerializeField] private TMP_Text _text;
    
    private void Awake()
    {
        _view.OnInitializeEvent += OnInitialize;
    }

    private void OnInitialize(Player player)
    {
        _text.text = player.NickName;
    }
}
