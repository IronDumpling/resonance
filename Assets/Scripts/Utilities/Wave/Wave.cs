using UnityEngine;
using Resonance.Utilities;
using DG.Tweening;

namespace Resonance.Core.Data
{
    /// <summary>
    /// QTE configuration data structure
    /// Used for wave resonance QTE mechanics
    /// </summary>
    [System.Serializable]
    public class QTEConfig
    {
        public Ease easeType;
        public float cycleDuration;
        public float targetWindow;

        public QTEConfig()
        {
            easeType = Ease.Linear;
            cycleDuration = 2f;
            targetWindow = 0.2f;
        }

        public QTEConfig(Ease easeType, float cycleDuration, float targetWindow)
        {
            this.easeType = easeType;
            this.cycleDuration = cycleDuration;
            this.targetWindow = targetWindow;
        }
    }

    /// <summary>
    /// Wave system - manages chaos and QTE configuration
    /// Part of CrystalCore system
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        #region Serialized Fields
        
        [Header("Wave Chaos")]
        [SerializeField] private float _currentChaos;
        [SerializeField] private float _maxChaos;
        [SerializeField] private float _chaosThreshold = 18f;
        [SerializeField] private WaveChaosState _chaosState;
        
        [Header("QTE Configuration")]
        [SerializeField] private QTEConfig _qteConfig;
        
        #endregion
        
        #region Properties
        
        public float CurrentChaos => _currentChaos;
        public float MaxChaos => _maxChaos;
        public float ChaosThreshold => _chaosThreshold;
        public WaveChaosState ChaosState => _chaosState;
        public float ChaosPercentage => _maxChaos > 0 ? _currentChaos / _maxChaos : 0f;
        public QTEConfig QTE => _qteConfig;
        
        #endregion
        
        #region Events
        
        public System.Action<float, float> OnChaosChanged; // current, max
        public System.Action<WaveChaosState> OnChaosStateChanged;
        
        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Wave(float maxChaos, float chaosThreshold, QTEConfig qteConfig = null)
        {
            _maxChaos = maxChaos;
            _chaosThreshold = chaosThreshold;
            _currentChaos = 0f;
            _chaosState = WaveChaosState.Order;
            _qteConfig = qteConfig ?? new QTEConfig();
        }
        
        #region Chaos Methods
        
        /// <summary>
        /// Add chaos value
        /// </summary>
        public float AddChaos(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousChaos = _currentChaos;
            _currentChaos = Mathf.Min(_currentChaos + amount, _maxChaos);
            float actualAdded = _currentChaos - previousChaos;

            if (actualAdded > 0f)
            {
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
                Debug.Log($"Wave: Added {actualAdded} chaos. Current: {_currentChaos}/{_maxChaos}");
            }

            return actualAdded;
        }
        
        /// <summary>
        /// Update chaos (natural recovery, called every frame)
        /// </summary>
        public void UpdateChaos(float chaosRecoveryRate, float deltaTime)
        {
            if (chaosRecoveryRate >= 0f || _currentChaos <= 0f) return;

            float previousChaos = _currentChaos;
            _currentChaos = Mathf.Max(0f, _currentChaos + chaosRecoveryRate * deltaTime);
            
            if (_currentChaos != previousChaos)
            {
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
            }
        }
        
        /// <summary>
        /// Update chaos state
        /// </summary>
        private void UpdateChaosState()
        {
            var previousState = _chaosState;
            
            if (_currentChaos >= _maxChaos)
            {
                _chaosState = WaveChaosState.Chaos;
            }
            else if (_currentChaos < _chaosThreshold)
            {
                _chaosState = WaveChaosState.Order;
            }
            // Maintain current state if between threshold and max

            if (previousState != _chaosState)
            {
                OnChaosStateChanged?.Invoke(_chaosState);
                Debug.Log($"Wave: Chaos state changed to {_chaosState}");
            }
        }
        
        /// <summary>
        /// Reset chaos to 0
        /// </summary>
        public void ResetChaos()
        {
            if (_currentChaos > 0f)
            {
                _currentChaos = 0f;
                UpdateChaosState();
                OnChaosChanged?.Invoke(_currentChaos, _maxChaos);
                Debug.Log("Wave: Chaos reset to 0");
            }
        }
        
        #endregion
        
        #region QTE Methods
        
        /// <summary>
        /// Set QTE configuration
        /// </summary>
        public void SetQTEConfig(QTEConfig config)
        {
            if (config != null)
            {
                _qteConfig = config;
                Debug.Log($"Wave: QTE config updated - Ease: {config.easeType}, Duration: {config.cycleDuration}, Window: {config.targetWindow}");
            }
        }
        
        /// <summary>
        /// Get QTE configuration
        /// </summary>
        public QTEConfig GetQTEConfig()
        {
            return _qteConfig;
        }
        
        #endregion
        
        #region Save/Load
        
        /// <summary>
        /// Get save data
        /// </summary>
        public WaveSaveData GetSaveData()
        {
            return new WaveSaveData
            {
                currentChaos = _currentChaos
            };
        }
        
        /// <summary>
        /// Load from save data
        /// </summary>
        public void LoadFromSaveData(WaveSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("Wave: Cannot load from null save data");
                return;
            }

            _currentChaos = Mathf.Clamp(saveData.currentChaos, 0f, _maxChaos);
            UpdateChaosState();
            OnChaosChanged?.Invoke(_currentChaos, _maxChaos);

            Debug.Log($"Wave: Loaded from save data. Chaos: {_currentChaos}/{_maxChaos}");
        }
        
        #endregion
        
        /// <summary>
        /// Cleanup event subscriptions
        /// </summary>
        public void Cleanup()
        {
            OnChaosChanged = null;
            OnChaosStateChanged = null;
        }
    }

    /// <summary>
    /// Wave save data structure
    /// </summary>
    [System.Serializable]
    public class WaveSaveData
    {
        public float currentChaos;

        public WaveSaveData()
        {
            currentChaos = 0f;
        }
    }
}
