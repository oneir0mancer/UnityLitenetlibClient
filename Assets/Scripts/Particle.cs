using UnityEngine;

public class Particle : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;
    
    private void Awake()
    {
        Destroy(this.gameObject, _particle.main.duration);
    }
}
