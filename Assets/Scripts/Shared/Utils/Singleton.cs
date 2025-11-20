using UnityEngine;

namespace Resonance.Utilities
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindFirstObjectByType<T>();
                            
                            if (_instance == null)
                            {
                                GameObject singletonObject = new GameObject(typeof(T).Name);
                                _instance = singletonObject.AddComponent<T>();
                                DontDestroyOnLoad(singletonObject);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"Multiple instances of singleton {typeof(T).Name} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
