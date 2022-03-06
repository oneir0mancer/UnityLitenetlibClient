using LiteNetLib.Utils;
using NetworkingLayer.Client;
using NetworkingLayer.Shared;
using UnityEngine;
using NetworkView = NetworkingLayer.Client.NetworkView;

public class NetworkPositionView : MonoBehaviour, ISerializeStream
{
    public int EntityId => _networkView.EntityId;
    public bool IsLocal => _networkView.IsLocal;
    
    [SerializeField] private NetworkView _networkView;

    [SerializeField] private bool _synchronizePosition = true;
    [SerializeField, Min(0)] private float _maxMovementSpeed = 5;
    [SerializeField, Min(0)] private float _minTeleportDistance = 3;
    [SerializeField] private bool _synchronizeRotation = true;
    [SerializeField, Min(0)] private float _maxRotationSpeed = 1;
    
    private Vector3 _lerpPosition;
    private float _lerpRotation;

    private void Start()    //TODO _networkView.OnInitializeEvent ?
    {
        Client.AddCallbackTarget(this);
    }

    private void OnDestroy()
    {
        Client.RemoveCallbackTarget(this);
    }
    
    private void Update()
    {
        if (!_networkView.IsLocal)
        {
            SynchronizePosition();
            SynchronizeRotation();
        }
    }

    private void SynchronizePosition()
    {
        if (!_synchronizePosition) return;
        
        if (Vector3.Distance(transform.position, _lerpPosition) > _minTeleportDistance)
            transform.position = _lerpPosition;
        else
            transform.position = Vector3.MoveTowards(transform.position, _lerpPosition, _maxMovementSpeed * Time.deltaTime);
    }

    public void SynchronizeRotation()
    {
        if (!_synchronizeRotation) return;
        
        float currentAngle = transform.rotation.eulerAngles.y;
        float angle = Mathf.MoveTowardsAngle(currentAngle, _lerpRotation, Mathf.Rad2Deg * _maxRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    void ISerializeStream.SendStream(NetDataWriter writer)
    {
        if (_synchronizePosition)
        {
            Vector2 position = new Vector2(transform.position.x, transform.position.z);
            writer.Put(position);
        }

        if (_synchronizeRotation)
        {
            float rotation = transform.rotation.eulerAngles.y;
            writer.Put(rotation);
        }
    }

    void ISerializeStream.ReceiveStream(NetDataReader reader)
    {
        if (_synchronizePosition)
        {
            Vector2 position = reader.GetVector2();
            _lerpPosition = new Vector3(position.x, 0, position.y);
        }
        
        if (_synchronizeRotation)
        {
            _lerpRotation = reader.GetFloat();
        }
    }
}
