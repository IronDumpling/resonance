using UnityEngine;
using Resonance.Core;
using Resonance.Player;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Player
{
    /// <summary>
    /// Handles player spawning in scenes. Automatically spawns player when scene loads.
    /// Can be configured with different spawn points based on how player entered the scene.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private Transform _defaultSpawnPoint;
        [SerializeField] private SpawnEntry[] _namedSpawnPoints;
        [SerializeField] private bool _autoSpawnOnStart = true;
        [SerializeField] private string _defaultSpawnID = "default";

        [Header("Player Prefab")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private string _playerPrefabResourcePath = "Prefabs/Player/Player";

        private IPlayerService _playerService;

        [System.Serializable]
        public class SpawnEntry
        {
            public string spawnID;
            public Transform spawnPoint;
            public string description;
        }

        void Start()
        {
            // Get PlayerService
            _playerService = ServiceRegistry.Get<IPlayerService>();
            if (_playerService == null)
            {
                Debug.LogError("PlayerSpawner: PlayerService not found!");
                return;
            }

            // Register spawn points
            RegisterSpawnPoints();

            // Auto-spawn if enabled
            if (_autoSpawnOnStart)
            {
                SpawnPlayer(_defaultSpawnID);
            }

            Debug.Log($"PlayerSpawner: Initialized in scene {gameObject.scene.name}");
        }

        private void RegisterSpawnPoints()
        {
            // Register default spawn point
            if (_defaultSpawnPoint != null)
            {
                _playerService.SetSpawnPoint(_defaultSpawnID, 
                    _defaultSpawnPoint.position, 
                    _defaultSpawnPoint.eulerAngles);
            }

            // Register named spawn points
            int namedPointsCount = 0;
            if (_namedSpawnPoints != null)
            {
                foreach (var entry in _namedSpawnPoints)
                {
                    if (entry?.spawnPoint != null && !string.IsNullOrEmpty(entry.spawnID))
                    {
                        _playerService.SetSpawnPoint(entry.spawnID,
                            entry.spawnPoint.position,
                            entry.spawnPoint.eulerAngles);
                        namedPointsCount++;
                    }
                }
            }

            Debug.Log($"PlayerSpawner: Registered {namedPointsCount + 1} spawn points");
        }

        public void SpawnPlayer(string spawnPointID = null)
        {
            if (_playerService == null) return;

            string spawnID = spawnPointID ?? _defaultSpawnID;

            // Check if player already exists in scene
            PlayerMonoBehaviour existingPlayer = FindAnyObjectByType<PlayerMonoBehaviour>();
            if (existingPlayer != null)
            {
                // Move existing player to spawn point
                MovePlayerToSpawnPoint(existingPlayer, spawnID);
                Debug.Log($"PlayerSpawner: Moved existing player to spawn point {spawnID}");
            }
            else
            {
                // Create new player
                SpawnNewPlayer(spawnID);
            }
        }

        private void SpawnNewPlayer(string spawnID)
        {
            // Get spawn position
            Vector3 spawnPosition = _defaultSpawnPoint != null ? _defaultSpawnPoint.position : Vector3.zero;
            Vector3 spawnRotation = _defaultSpawnPoint != null ? _defaultSpawnPoint.eulerAngles : Vector3.zero;

            // Find specific spawn point
            if (_namedSpawnPoints != null)
            {
                foreach (var entry in _namedSpawnPoints)
                {
                    if (entry?.spawnID == spawnID && entry?.spawnPoint != null)
                    {
                        spawnPosition = entry.spawnPoint.position;
                        spawnRotation = entry.spawnPoint.eulerAngles;
                        break;
                    }
                }
            }

            CreatePlayerAtPosition(spawnPosition, spawnRotation);
            Debug.Log($"PlayerSpawner: Spawned new player at {spawnID} ({spawnPosition})");
        }

        public void SpawnPlayerAtPosition(Vector3 position, Vector3 rotation)
        {
            // Check if player already exists in scene
            PlayerMonoBehaviour existingPlayer = FindAnyObjectByType<PlayerMonoBehaviour>();
            if (existingPlayer != null)
            {
                existingPlayer.SetPosition(position);
                existingPlayer.SetRotation(rotation);
            }
            else
            {
                // Create new player at position
                CreatePlayerAtPosition(position, rotation);
            }
        }

        private void MovePlayerToSpawnPoint(PlayerMonoBehaviour player, string spawnID)
        {
            Vector3 spawnPosition = _defaultSpawnPoint != null ? _defaultSpawnPoint.position : Vector3.zero;
            Vector3 spawnRotation = _defaultSpawnPoint != null ? _defaultSpawnPoint.eulerAngles : Vector3.zero;

            // Find specific spawn point
            if (_namedSpawnPoints != null)
            {
                foreach (var entry in _namedSpawnPoints)
                {
                    if (entry?.spawnID == spawnID && entry?.spawnPoint != null)
                    {
                        spawnPosition = entry.spawnPoint.position;
                        spawnRotation = entry.spawnPoint.eulerAngles;
                        break;
                    }
                }
            }

            player.SetPosition(spawnPosition);
            player.SetRotation(spawnRotation);
        }

        private void CreatePlayerAtPosition(Vector3 position, Vector3 rotation)
        {
            // Try to get prefab
            GameObject prefabToUse = _playerPrefab;
            if (prefabToUse == null && !string.IsNullOrEmpty(_playerPrefabResourcePath))
            {
                prefabToUse = Resources.Load<GameObject>(_playerPrefabResourcePath);
            }

            if (prefabToUse == null)
            {
                Debug.LogError("PlayerSpawner: No player prefab available!");
                return;
            }

            // Instantiate player
            GameObject playerInstance = Instantiate(prefabToUse, position, Quaternion.Euler(rotation));
            playerInstance.name = "Player";

            Debug.Log($"PlayerSpawner: Created new player at {position}");
        }

        // Public methods for external triggers
        public void SpawnAtDefault() => SpawnPlayer(_defaultSpawnID);
        public void SpawnAtEntry(string entryID) => SpawnPlayer(entryID);

        #region Editor Support

        void OnDrawGizmosSelected()
        {
            // Draw default spawn point
            if (_defaultSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_defaultSpawnPoint.position, Vector3.one);
                Gizmos.DrawRay(_defaultSpawnPoint.position, _defaultSpawnPoint.forward * 2f);
            }

            // Draw named spawn points (safe null check)
            if (_namedSpawnPoints != null)
            {
                foreach (var entry in _namedSpawnPoints)
                {
                    if (entry?.spawnPoint != null)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(entry.spawnPoint.position, Vector3.one * 0.8f);
                        Gizmos.DrawRay(entry.spawnPoint.position, entry.spawnPoint.forward * 1.5f);
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            // Always show default spawn
            if (_defaultSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_defaultSpawnPoint.position + Vector3.up * 0.1f, Vector3.one * 0.5f);
            }
        }

        #endregion
    }
}
