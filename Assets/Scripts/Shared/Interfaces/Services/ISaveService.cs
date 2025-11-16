using System;
using System.Collections.Generic;
using Resonance.Player.Data;

namespace Resonance.Interfaces.Services
{
    /// <summary>
    /// Interface for the save/load system.
    /// Handles persistent storage of player data and game state.
    /// </summary>
    public interface ISaveService : IGameService
    {
        // Events
        event Action<PlayerSaveData> OnSaveCompleted;
        event Action<PlayerSaveData> OnLoadCompleted;
        event Action<string> OnSaveDeleted;

        // Save Management
        bool HasSaveData { get; }
        PlayerSaveData CurrentSaveData { get; }
        DateTime LastSaveTime { get; }
        
        // Save Operations
        void SaveGame(string savePointID);
        void SaveGame(PlayerSaveData saveData);
        bool LoadGame(string saveID);
        bool LoadLastSave();
        void DeleteSave(string saveID);
        void DeleteAllSaves();

        // Save File Management
        List<SaveFileInfo> GetAllSaves();
        bool SaveExists(string saveID);
        long GetSaveFileSize(string saveID);

        // Quick Save/Load
        void QuickSave();
        bool QuickLoad();
        bool HasQuickSave { get; }
    }

    /// <summary>
    /// Information about a save file
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string saveID;
        public string sceneName;
        public DateTime saveTime;
        public float playTime;
        public long fileSize;

        public SaveFileInfo(PlayerSaveData saveData, long size)
        {
            saveID = saveData.saveID;
            sceneName = saveData.sceneName;
            saveTime = DateTime.FromBinary((long)saveData.saveTimestamp);
            fileSize = size;
        }
    }
}
