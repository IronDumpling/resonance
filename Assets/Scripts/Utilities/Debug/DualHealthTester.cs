using UnityEngine;
using Resonance.Player.Core;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Testing utility for the dual health system.
    /// This component provides editor buttons to test physical and mental damage/healing.
    /// </summary>
    public class DualHealthTester : MonoBehaviour
    {
        [Header("Test Values")]
        [SerializeField] private float _physicalDamageAmount = 10f;
        [SerializeField] private float _mentalDamageAmount = 5f;
        [SerializeField] private float _physicalHealAmount = 15f;
        [SerializeField] private float _mentalHealAmount = 10f;

        [Header("References")]
        [SerializeField] private PlayerMonoBehaviour _playerMonoBehaviour;

        private PlayerController _playerController;

        void Start()
        {
            // Auto-find player if not assigned
            if (_playerMonoBehaviour == null)
            {
                _playerMonoBehaviour = FindAnyObjectByType<PlayerMonoBehaviour>();
            }

            if (_playerMonoBehaviour != null)
            {
                _playerController = _playerMonoBehaviour.Controller;
                
                // Subscribe to health events for testing
                if (_playerController != null)
                {
                    _playerController.OnPhysicalHealthChanged += OnPhysicalHealthChanged;
                    _playerController.OnMentalHealthChanged += OnMentalHealthChanged;
                    _playerController.OnPhysicalDeath += OnPhysicalDeath;
                    _playerController.OnTrueDeath += OnTrueDeath;
                }
            }
            else
            {
                Debug.LogWarning("DualHealthTester: No PlayerMonoBehaviour found!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_playerController != null)
            {
                _playerController.OnPhysicalHealthChanged -= OnPhysicalHealthChanged;
                _playerController.OnMentalHealthChanged -= OnMentalHealthChanged;
                _playerController.OnPhysicalDeath -= OnPhysicalDeath;
                _playerController.OnTrueDeath -= OnTrueDeath;
            }
        }

        #region Test Methods

        [ContextMenu("Deal Physical Damage")]
        public void TestPhysicalDamage()
        {
            if (_playerMonoBehaviour != null)
            {
                _playerMonoBehaviour.TakePhysicalDamage(_physicalDamageAmount);
                Debug.Log($"DualHealthTester: Dealt {_physicalDamageAmount} physical damage");
            }
        }

        [ContextMenu("Deal Mental Damage")]
        public void TestMentalDamage()
        {
            if (_playerMonoBehaviour != null)
            {
                _playerMonoBehaviour.TakeMentalDamage(_mentalDamageAmount);
                Debug.Log($"DualHealthTester: Dealt {_mentalDamageAmount} mental damage");
            }
        }

        [ContextMenu("Heal Physical Health")]
        public void TestPhysicalHeal()
        {
            if (_playerMonoBehaviour != null)
            {
                _playerMonoBehaviour.HealPhysical(_physicalHealAmount);
                Debug.Log($"DualHealthTester: Healed {_physicalHealAmount} physical health");
            }
        }

        [ContextMenu("Heal Mental Health")]
        public void TestMentalHeal()
        {
            if (_playerMonoBehaviour != null)
            {
                _playerMonoBehaviour.HealMental(_mentalHealAmount);
                Debug.Log($"DualHealthTester: Healed {_mentalHealAmount} mental health");
            }
        }

        [ContextMenu("Restore All Health")]
        public void TestRestoreAllHealth()
        {
            if (_playerController != null)
            {
                _playerController.RestoreToFullHealth();
                Debug.Log("DualHealthTester: Restored all health to full");
            }
        }

        [ContextMenu("Kill Physical Health")]
        public void TestKillPhysicalHealth()
        {
            if (_playerController != null)
            {
                float damage = _playerController.Stats.currentPhysicalHealth + 1f;
                _playerMonoBehaviour.TakePhysicalDamage(damage);
                Debug.Log($"DualHealthTester: Killed physical health with {damage} damage");
            }
        }

        [ContextMenu("Kill Mental Health")]
        public void TestKillMentalHealth()
        {
            if (_playerController != null)
            {
                float damage = _playerController.Stats.currentMentalHealth + 1f;
                _playerMonoBehaviour.TakeMentalDamage(damage);
                Debug.Log($"DualHealthTester: Killed mental health with {damage} damage");
            }
        }

        [ContextMenu("Print Health Status")]
        public void PrintHealthStatus()
        {
            if (_playerController != null)
            {
                var stats = _playerController.Stats;
                Debug.Log($"Health Status:\n" +
                         $"Physical: {stats.currentPhysicalHealth:F1}/{stats.maxPhysicalHealth:F1} ({stats.PhysicalHealthPercentage:P1})\n" +
                         $"Mental: {stats.currentMentalHealth:F1}/{stats.maxMentalHealth:F1} ({stats.MentalHealthPercentage:P1})\n" +
                         $"IsPhysicallyAlive: {_playerController.IsPhysicallyAlive}\n" +
                         $"IsMentallyAlive: {_playerController.IsMentallyAlive}\n" +
                         $"IsMentallyAlive: {_playerController.IsMentallyAlive}\n" +
                         $"IsInPhysicalDeathState: {_playerController.IsInPhysicalDeathState}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnPhysicalHealthChanged(float current, float max)
        {
            Debug.Log($"<color=red>Physical Health Changed: {current:F1}/{max:F1} ({(current/max):P1})</color>");
        }

        private void OnMentalHealthChanged(float current, float max)
        {
            // Debug.Log($"<color=blue>Mental Health Changed: {current:F1}/{max:F1} ({(current/max):P1})</color>");
        }

        private void OnPhysicalDeath()
        {
            Debug.Log("<color=orange>PHYSICAL DEATH EVENT TRIGGERED - Player entering core mode!</color>");
        }

        private void OnTrueDeath()
        {
            Debug.Log("<color=red>TRUE DEATH EVENT TRIGGERED - Game over!</color>");
        }

        #endregion

        #region GUI Testing (for runtime testing)

        void OnGUI()
        {
            if (_playerController == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("Dual Health System Tester", GUI.skin.box);

            // Display current health
            var stats = _playerController.Stats;
            GUILayout.Label($"Physical: {stats.currentPhysicalHealth:F1}/{stats.maxPhysicalHealth:F1}");
            GUILayout.Label($"Mental: {stats.currentMentalHealth:F1}/{stats.maxMentalHealth:F1}");
            
            GUILayout.Space(10);

            // Test buttons
            if (GUILayout.Button("Physical Damage"))
                TestPhysicalDamage();
            
            if (GUILayout.Button("Mental Damage"))
                TestMentalDamage();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Heal Physical"))
                TestPhysicalHeal();
            
            if (GUILayout.Button("Heal Mental"))
                TestMentalHeal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Restore All"))
                TestRestoreAllHealth();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Kill Physical"))
                TestKillPhysicalHealth();
            
            if (GUILayout.Button("Kill Mental"))
                TestKillMentalHealth();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Print Status"))
                PrintHealthStatus();

            GUILayout.EndArea();
        }

        #endregion
    }
}
