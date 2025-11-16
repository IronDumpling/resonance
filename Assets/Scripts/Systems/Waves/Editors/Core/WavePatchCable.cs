using System;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Represents a patch cable connection between two module ports
    /// Patch cables are edges in the module graph
    /// </summary>
    [Serializable]
    public class WavePatchCable
    {
        /// <summary>
        /// Unique identifier for this patch cable
        /// </summary>
        public string cableID;
        
        /// <summary>
        /// Source module ID
        /// </summary>
        public string sourceModuleID;
        
        /// <summary>
        /// Source port ID (output port)
        /// </summary>
        public string sourcePortID;
        
        /// <summary>
        /// Target module ID
        /// </summary>
        public string targetModuleID;
        
        /// <summary>
        /// Target port ID (input port)
        /// </summary>
        public string targetPortID;
        
        /// <summary>
        /// Stack index for this cable (when multiple cables connect to same output)
        /// Used to determine processing order
        /// </summary>
        public int stackIndex;
        
        // Properties for backward compatibility
        public string CableID { get => cableID; set => cableID = value; }
        public string SourceModuleID { get => sourceModuleID; set => sourceModuleID = value; }
        public string SourcePortID { get => sourcePortID; set => sourcePortID = value; }
        public string TargetModuleID { get => targetModuleID; set => targetModuleID = value; }
        public string TargetPortID { get => targetPortID; set => targetPortID = value; }
        public int StackIndex { get => stackIndex; set => stackIndex = value; }
        
        public WavePatchCable()
        {
            cableID = Guid.NewGuid().ToString();
            stackIndex = 0;
        }
        
        public WavePatchCable(string sourceModuleID, string sourcePortID, string targetModuleID, string targetPortID)
        {
            cableID = Guid.NewGuid().ToString();
            this.sourceModuleID = sourceModuleID;
            this.sourcePortID = sourcePortID;
            this.targetModuleID = targetModuleID;
            this.targetPortID = targetPortID;
            stackIndex = 0;
        }
        
        /// <summary>
        /// Check if this cable connects the specified ports
        /// </summary>
        public bool Connects(string sourceModuleID, string sourcePortID, string targetModuleID, string targetPortID)
        {
            return this.sourceModuleID == sourceModuleID &&
                   this.sourcePortID == sourcePortID &&
                   this.targetModuleID == targetModuleID &&
                   this.targetPortID == targetPortID;
        }
        
        /// <summary>
        /// Check if this cable is connected to the specified module
        /// </summary>
        public bool IsConnectedToModule(string moduleID)
        {
            return sourceModuleID == moduleID || targetModuleID == moduleID;
        }
    }
}

