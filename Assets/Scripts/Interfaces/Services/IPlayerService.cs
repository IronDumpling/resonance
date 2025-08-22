using Resonance.Player;
using Resonance.Player.Data;

namespace Resonance.Core
{
    /// <summary>
    /// Global service interface for managing the player across scenes.
    /// Handles player persistence, spawn points, and cross-scene continuity.
    /// </summary>
    public interface IPlayerService : IGameService
    {
        // Events
        event System.Action<PlayerMonoBehaviour> OnPlayerRegistered;
        event System.Action OnPlayerUnregistered;
        event System.Action<PlayerSaveData> OnPlayerSaved;
        event System.Action<PlayerSaveData> OnPlayerLoaded;

        // Player Management
        PlayerMonoBehaviour CurrentPlayer { get; }
        bool HasPlayer { get; }
        void RegisterPlayer(PlayerMonoBehaviour player);
        void UnregisterPlayer();

        // Save/Load
        void SavePlayerState(string savePointID);
        void LoadPlayerState(PlayerSaveData saveData);
        PlayerSaveData GetCurrentPlayerState();

        // Spawn Management
        void SetSpawnPoint(string sceneSpawnID, UnityEngine.Vector3 position, UnityEngine.Vector3 rotation);
        void SpawnPlayerAtPoint(string sceneSpawnID);
        void SpawnPlayerAtPosition(UnityEngine.Vector3 position, UnityEngine.Vector3 rotation);

        // Player State Queries
        bool IsPlayerAlive();
        float GetPlayerHealth();
        int GetPlayerLevel();
    }
}
