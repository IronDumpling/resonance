using UnityEngine;
using System.Collections.Generic;
using Resonance.Player;
using Resonance.Player.Data;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Global service for managing the player across scenes.
    /// Handles player persistence, spawn management, and cross-scene continuity.
    /// </summary>
    public class PlayerService : IPlayerService
    {
        public int Priority => 20; // After InputService and UIService
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        // Events
        public event System.Action<PlayerMonoBehaviour> OnPlayerRegistered;
        public event System.Action OnPlayerUnregistered;
        public event System.Action<PlayerSaveData> OnPlayerSaved;
        public event System.Action<PlayerSaveData> OnPlayerLoaded;

        // Player Management
        private PlayerMonoBehaviour _currentPlayer;
        private Dictionary<string, SpawnPoint> _spawnPoints = new Dictionary<string, SpawnPoint>();
        private PlayerSaveData _persistentPlayerData;

        // Properties
        public PlayerMonoBehaviour CurrentPlayer => _currentPlayer;
        public bool HasPlayer => _currentPlayer != null;

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("PlayerService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("PlayerService: Initializing");

            State = SystemState.Running;
            Debug.Log("PlayerService: Initialized successfully");
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown) return;

            Debug.Log("PlayerService: Shutting down");

            // Clean up
            if (_currentPlayer != null)
            {
                UnregisterPlayer();
            }

            _spawnPoints.Clear();
            _persistentPlayerData = null;

            // Clear events
            OnPlayerRegistered = null;
            OnPlayerUnregistered = null;
            OnPlayerSaved = null;
            OnPlayerLoaded = null;

            State = SystemState.Shutdown;
        }

        #region Player Management

        public void RegisterPlayer(PlayerMonoBehaviour player)
        {
            if (player == null)
            {
                Debug.LogError("PlayerService: Cannot register null player");
                return;
            }

            if (_currentPlayer != null)
            {
                Debug.LogWarning("PlayerService: Player already registered. Unregistering previous player.");
                UnregisterPlayer();
            }

            _currentPlayer = player;
            OnPlayerRegistered?.Invoke(player);

            // Load persistent data if available
            if (_persistentPlayerData != null)
            {
                LoadPlayerState(_persistentPlayerData);
            }

            Debug.Log($"PlayerService: Registered player {player.name}");
        }

        public void UnregisterPlayer()
        {
            if (_currentPlayer == null) return;

            Debug.Log($"PlayerService: Unregistering player {_currentPlayer.name}");
            
            _currentPlayer = null;
            OnPlayerUnregistered?.Invoke();
        }

        #endregion

        #region Save/Load

        public void SavePlayerState(string savePointID)
        {
            if (_currentPlayer == null || !_currentPlayer.IsInitialized)
            {
                Debug.LogWarning("PlayerService: No player to save");
                return;
            }

            _persistentPlayerData = _currentPlayer.CreateSaveData(savePointID);
            OnPlayerSaved?.Invoke(_persistentPlayerData);

            Debug.Log($"PlayerService: Saved player state at {savePointID}");
        }

        public void LoadPlayerState(PlayerSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("PlayerService: Cannot load null save data");
                return;
            }

            _persistentPlayerData = saveData;

            if (_currentPlayer != null && _currentPlayer.IsInitialized)
            {
                _currentPlayer.LoadFromSaveData(saveData);
                OnPlayerLoaded?.Invoke(saveData);
                Debug.Log($"PlayerService: Loaded player state from {saveData.saveID}");
            }
            else
            {
                Debug.Log("PlayerService: Save data stored, will be applied when player is registered");
            }
        }

        public PlayerSaveData GetCurrentPlayerState()
        {
            if (_currentPlayer != null && _currentPlayer.IsInitialized)
            {
                return _currentPlayer.CreateSaveData("current_state");
            }

            return _persistentPlayerData;
        }

        #endregion

        #region Spawn Management

        public void SetSpawnPoint(string sceneSpawnID, Vector3 position, Vector3 rotation)
        {
            _spawnPoints[sceneSpawnID] = new SpawnPoint
            {
                position = position,
                rotation = rotation,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };

            Debug.Log($"PlayerService: Set spawn point {sceneSpawnID} at {position}");
        }

        public void SpawnPlayerAtPoint(string sceneSpawnID)
        {
            if (!_spawnPoints.TryGetValue(sceneSpawnID, out SpawnPoint spawnPoint))
            {
                Debug.LogError($"PlayerService: Spawn point {sceneSpawnID} not found");
                return;
            }

            SpawnPlayerAtPosition(spawnPoint.position, spawnPoint.rotation);
        }

        public void SpawnPlayerAtPosition(Vector3 position, Vector3 rotation)
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.SetPosition(position);
                _currentPlayer.SetRotation(rotation);
                Debug.Log($"PlayerService: Moved existing player to {position}");
            }
            else
            {
                Debug.LogWarning("PlayerService: No player to move. Player should be spawned by scene's PlayerSpawner.");
            }
        }

        #endregion

        #region Player State Queries

        public bool IsPlayerPhysicallyAlive()
        {
            return _currentPlayer != null && _currentPlayer.IsInitialized && _currentPlayer.Controller.IsPhysicallyAlive;
        }

        public bool IsPlayerMentallyAlive()
        {
            return _currentPlayer != null && _currentPlayer.IsInitialized && _currentPlayer.Controller.IsMentallyAlive;
        }

        public float GetPlayerHealth()
        {
            if (_currentPlayer?.IsInitialized == true)
            {
                return _currentPlayer.Controller.Stats.currentHealth;
            }
            return 0f;
        }

        public int GetPlayerLevel()
        {
            if (_currentPlayer?.IsInitialized == true)
            {
                return _currentPlayer.Controller.Level;
            }
            return 1;
        }

        #endregion

        #region Helper Classes

        [System.Serializable]
        private class SpawnPoint
        {
            public Vector3 position;
            public Vector3 rotation;
            public string sceneName;
        }

        #endregion
    }
}
