using UnityEngine;

namespace Utils
{
    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    T[] assets = Resources.LoadAll<T>("");
                    if (assets != null && assets.Length > 0)
                    {
                        if (assets.Length > 1)
                        {
                            Debug.LogError($"Found more than one instance of {typeof(T)} singleton SO");
                            foreach (var asset in assets)
                            {
                                Debug.LogError(asset.name + " | " + asset.ToString());
                            }
                        }
                        _instance = assets[0];
                    }
                    else
                    {
                        _instance = CreateInstance<T>();
                    }
                }

                return _instance;
            }
        }
    
        public static bool E_Exists()
        {
            return _instance != null;
        }

        protected virtual void OnEnable()
        {
            if (_instance == null) 
            {
                _instance = this as T;
            } 
            else if (_instance != this as T)
            {
                Debug.LogError($"Trying to assign multiple instances of {typeof(T)} singleton SO", this);
            }
        }

        protected virtual void OnDisable()
        {
            _instance = null;
        }
    }
}
