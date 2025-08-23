using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

namespace Resonance.Core
{
    [CreateAssetMenu(fileName = "ServiceConfiguration", menuName = "Resonance/Service Configuration")]
    public class ServiceConfiguration : ScriptableObject
    {
        [Header("Input System Configuration")]
        public InputActionAsset inputActions;
        
        [Header("Audio System Configuration")]
        public AudioMixerGroup masterMixerGroup;
        
        [Header("Save System Configuration")]
        public string saveFilePath = "SaveData";
        
        [Header("Resource System Configuration")]
        public bool useAddressables = true;
        
        // Future service configurations can be added here
        // This centralizes all service configuration in one asset
    }
}
