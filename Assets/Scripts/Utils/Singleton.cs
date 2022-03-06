using UnityEngine;

namespace Utils
{
    public abstract class Singleton<T> : MonoBehaviour where T : Component {

        private static T _instance;
    
        public static bool E_Exists()
        {
            return _instance != null;
        }
    
        public static T Instance 
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake() {
            if (_instance == null) {
                _instance = this as T;
            } else if (_instance != this as T) {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
} 
