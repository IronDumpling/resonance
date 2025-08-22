using UnityEngine;
using Resonance.Utilities;
using Resonance.Core;

namespace Resonance.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Configuration")]
        [SerializeField] private ServiceConfiguration _serviceConfiguration;
        
        [Header("Core Systems")]
        private Services _services;
        private StateMachine.GameStateMachine _stateMachine;

        public Services Services => _services;
        public StateMachine.GameStateMachine StateMachine => _stateMachine;
        public ServiceConfiguration Configuration => _serviceConfiguration;

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize Services as a member, not a component
            _services = new Services(gameObject, _serviceConfiguration);

            // StateMachine remains as a component for Update() lifecycle
            if (_stateMachine == null)
            {
                _stateMachine = GetComponent<StateMachine.GameStateMachine>();
                if (_stateMachine == null)
                {
                    _stateMachine = gameObject.AddComponent<StateMachine.GameStateMachine>();
                }
            }
        }

        void Start()
        {
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            Debug.Log("GameManager: Initializing core systems");
            
            // Initialize services first
            _services?.Initialize();
            
            // Initialize state machine after services
            _stateMachine?.Initialize();
            
            Debug.Log("GameManager: Core systems initialized");
        }

        void Update()
        {
            _stateMachine?.Update();
        }

        protected override void OnDestroy()
        {
            _stateMachine?.Shutdown();
            _services?.Shutdown();
            base.OnDestroy();
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        public void QuitGame()
        {
            Debug.Log("GameManager: Quitting game");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
