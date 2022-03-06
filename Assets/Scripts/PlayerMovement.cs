using UnityEngine;
using NetworkView = NetworkingLayer.Client.NetworkView;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private NetworkView _view;
    [SerializeField, Min(0)] private float _movementSpeed = 5;
    [SerializeField, Min(0)] private float _rotationSpeed = 1;
    
    private void Awake()
    {
        _view ??= GetComponent<NetworkView>();
    }

    private void Update()
    {
        if (!_view.IsLocal) return;
            
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (h == 0 && v == 0) return;

        Vector3 motion = _movementSpeed * Time.deltaTime * new Vector3(h, 0, v);
        transform.Translate(motion, Space.World);

        Quaternion rotation = new Quaternion();
        rotation.SetLookRotation(motion.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation,  Mathf.Rad2Deg * _rotationSpeed * Time.deltaTime);
    }
}
