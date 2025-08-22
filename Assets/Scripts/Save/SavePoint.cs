using UnityEngine;
using Resonance.Core;
using Resonance.Player;
using Resonance.Utilities;

namespace Resonance.Save
{
    /// <summary>
    /// Save point component that allows players to save their game.
    /// Restores player health to maximum when used.
    /// </summary>
    public class SavePoint : MonoBehaviour
    {
        [Header("Save Point Configuration")]
        [SerializeField] private string _savePointID;
        [SerializeField] private bool _autoSave = false; // Auto-save when player enters trigger
        [SerializeField] private bool _oneTimeUse = false; // Can only be used once
        [SerializeField] private float _interactionRange = 2f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _activeVisual;
        [SerializeField] private GameObject _usedVisual;
        [SerializeField] private ParticleSystem _saveEffect;

        [Header("Audio")]
        [SerializeField] private AudioClip _saveSound;

        private bool _hasBeenUsed = false;
        private bool _playerInRange = false;
        private PlayerMonoBehaviour _currentPlayer;
        private ISaveService _saveSystem;
        private IPlayerService _playerService;

        // Events
        public System.Action<string> OnSavePointUsed;

        // Properties
        public string SavePointID => _savePointID;
        public bool HasBeenUsed => _hasBeenUsed;
        public bool CanUse => !(_oneTimeUse && _hasBeenUsed);

        #region Unity Lifecycle

        void Start()
        {
            // Get services
            _saveSystem = ServiceRegistry.Get<ISaveService>();
            _playerService = ServiceRegistry.Get<IPlayerService>();

            // Auto-generate ID if not set
            if (string.IsNullOrEmpty(_savePointID))
            {
                _savePointID = $"SavePoint_{gameObject.scene.name}_{transform.position}";
            }

            // Initialize visuals
            UpdateVisuals();

            Debug.Log($"SavePoint: Initialized {_savePointID}");
        }

        void Update()
        {
            // Handle interaction input when player is in range
            if (_playerInRange && CanUse && Input.GetKeyDown(KeyCode.E)) // TODO: Use proper input action
            {
                UseSavePoint();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerMonoBehaviour>();
            if (player != null)
            {
                _currentPlayer = player;
                _playerInRange = true;

                if (_autoSave && CanUse)
                {
                    UseSavePoint();
                }

                Debug.Log($"SavePoint: Player entered range of {_savePointID}");
            }
        }

        void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<PlayerMonoBehaviour>();
            if (player != null && player == _currentPlayer)
            {
                _currentPlayer = null;
                _playerInRange = false;

                Debug.Log($"SavePoint: Player left range of {_savePointID}");
            }
        }

        #endregion

        #region Save Point Logic

        public void UseSavePoint()
        {
            if (!CanUse)
            {
                Debug.LogWarning($"SavePoint: Cannot use {_savePointID} - already used or disabled");
                return;
            }

            if (_currentPlayer == null || !_currentPlayer.IsInitialized)
            {
                Debug.LogWarning($"SavePoint: No valid player to save");
                return;
            }

            if (_saveSystem == null)
            {
                Debug.LogError($"SavePoint: SaveSystem not available");
                return;
            }

            // Restore player health to maximum
            _currentPlayer.Controller.RestoreToFullHealth();

            // Save the game
            _saveSystem.SaveGame(_savePointID);

            // Mark as used
            _hasBeenUsed = true;
            UpdateVisuals();

            // Play effects
            PlaySaveEffects();

            OnSavePointUsed?.Invoke(_savePointID);
            Debug.Log($"SavePoint: Used save point {_savePointID}");
        }

        private void UpdateVisuals()
        {
            if (_activeVisual != null)
            {
                _activeVisual.SetActive(CanUse);
            }

            if (_usedVisual != null)
            {
                _usedVisual.SetActive(_hasBeenUsed);
            }
        }

        private void PlaySaveEffects()
        {
            // Play particle effect
            if (_saveEffect != null)
            {
                _saveEffect.Play();
            }

            // Play sound effect
            if (_saveSound != null)
            {
                // TODO: Use proper audio system
                AudioSource.PlayClipAtPoint(_saveSound, transform.position);
            }
        }

        #endregion

        #region Editor Support

        void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);

            // Draw save point icon
            Gizmos.color = CanUse ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up, Vector3.one * 0.5f);
        }

        void OnValidate()
        {
            // Ensure interaction range is positive
            _interactionRange = Mathf.Max(0.1f, _interactionRange);
        }

        #endregion
    }
}
