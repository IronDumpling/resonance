using UnityEngine;
using Resonance.Gameplay.Enemies.Data;

namespace Resonance.Gameplay.Enemies.Triggers
{
    /// <summary>
    /// EnemyVision:
    /// Based on the vision angle and distance, the enemy will check if the player is within its vision cone.
    /// </summary>
    public class EnemyVision : MonoBehaviour
    {
        private Transform _enemyTransform;
        private EnemyRuntimeStats _stats;
        private Transform _playerTransform;
        private Vector3 _lastKnownPlayerPosition;
        private bool _hasLastKnownPosition = false;
        private bool _isInitialized = false;

        // Vision check points on player (relative offsets)
        private readonly Vector3[] _playerCheckPoints = new Vector3[]
        {
            new Vector3(0f, 0.5f, 0f),  // Head
            new Vector3(0f, 0f, 0f),  // Chest
            new Vector3(0f, -0.5f, 0f)   // Waist
        };

        /// <summary>
        /// Last known player position
        /// </summary>
        public Vector3 LastKnownPlayerPosition => _lastKnownPlayerPosition;

        /// <summary>
        /// Has last known position
        /// </summary>
        public bool HasLastKnownPosition => _hasLastKnownPosition;

        /// <summary>
        /// Initialize vision system
        /// </summary>
        /// <param name="enemyTransform">Enemy Transform</param>
        /// <param name="stats">Enemy Runtime Stats</param>
        public void Initialize(Transform enemyTransform, EnemyRuntimeStats stats)
        {
            _enemyTransform = enemyTransform;
            _stats = stats;
            _isInitialized = true;

            // Find player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                Debug.Log($"EnemyVision: Initialized and found player");
            }
            else
            {
                Debug.LogWarning($"EnemyVision: Initialized but no player found with tag 'Player'");
            }
        }

        /// <summary>
        /// Check if player is visible
        /// Includes angle check, distance check, and raycast obstruction check
        /// </summary>
        /// <returns>If player is visible</returns>
        public bool CanSeePlayer()
        {
            if (!_isInitialized || _enemyTransform == null || _stats == null)
            {
                return false;
            }

            // If no player found, try to find again
            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                }
                else
                {
                    return false;
                }
            }

            Vector3 playerPosition = _playerTransform.position;

            // 1. Distance Check (XZ plane horizontal distance)
            Vector3 enemyPos = _enemyTransform.position;
            Vector2 enemyPosXZ = new Vector2(enemyPos.x, enemyPos.z);
            Vector2 playerPosXZ = new Vector2(playerPosition.x, playerPosition.z);
            float horizontalDistance = Vector2.Distance(enemyPosXZ, playerPosXZ);
            
            if (horizontalDistance > _stats.visionDistance)
            {
                return false;
            }

            // 2. Height Check (relative to eye position)
            Vector3 eyePosition = _enemyTransform.position + Vector3.up * _stats.eyeHeightOffset;
            float heightDifference = Mathf.Abs(playerPosition.y - eyePosition.y);
            
            if (heightDifference > _stats.visionHeightRange)
            {
                return false;
            }

            // 3. Angle Check (XZ plane)
            Vector3 directionToPlayer = (playerPosition - _enemyTransform.position).normalized;
            Vector3 enemyForward = _enemyTransform.forward;
            
            // Project to XZ plane
            directionToPlayer.y = 0f;
            enemyForward.y = 0f;
            directionToPlayer.Normalize();
            enemyForward.Normalize();

            float angle = Vector3.Angle(enemyForward, directionToPlayer);
            if (angle > _stats.visionAngle * 0.5f)
            {
                return false;
            }

            // 4. Raycast Obstruction Check
            // Check from eye position to multiple points on player
            // QueryTriggerInteraction.Ignore - ignore trigger colliders, only detect solid colliders
            
            foreach (Vector3 checkPointOffset in _playerCheckPoints)
            {
                Vector3 targetPoint = playerPosition + checkPointOffset;
                Vector3 directionToTarget = targetPoint - eyePosition;
                float distanceToTarget = directionToTarget.magnitude;

                // Cast ray from eye to target point, ignoring trigger colliders
                RaycastHit hit;
                bool didHit = Physics.Raycast(eyePosition, directionToTarget.normalized, out hit, distanceToTarget, _stats.visionObstacleLayers, QueryTriggerInteraction.Ignore);
                
                if (didHit)
                {
                    // Check if we hit the player
                    if (hit.collider.CompareTag("Player"))
                    {
                        // Successfully see player, update last known position
                        _lastKnownPlayerPosition = playerPosition;
                        _hasLastKnownPosition = true;
                        return true;
                    }
                }
            }

            // All raycasts failed or hit obstacles
            return false;
        }

        /// <summary>
        /// Update last known player position (can be manually called when vision is lost)
        /// </summary>
        public void UpdateLastKnownPosition(Vector3 position)
        {
            _lastKnownPlayerPosition = position;
            _hasLastKnownPosition = true;
        }

        /// <summary>
        /// Clear last known position
        /// </summary>
        public void ClearLastKnownPosition()
        {
            _hasLastKnownPosition = false;
        }

        #region Debug Visualization

        /// <summary>
        /// Draw vision range for debugging
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (!_isInitialized || _enemyTransform == null || _stats == null)
                return;

            Vector3 eyePosition = _enemyTransform.position + Vector3.up * _stats.eyeHeightOffset;
            Vector3 forward = _enemyTransform.forward;

            // Draw vision cone
            Gizmos.color = Color.yellow;
            float halfAngle = _stats.visionAngle * 0.5f;
            
            // Left boundary
            Vector3 leftBoundary = Quaternion.Euler(0f, -halfAngle, 0f) * forward * _stats.visionDistance;
            Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary);
            
            // Right boundary
            Vector3 rightBoundary = Quaternion.Euler(0f, halfAngle, 0f) * forward * _stats.visionDistance;
            Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary);
            
            // Forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(eyePosition, eyePosition + forward * _stats.visionDistance);

            // Draw vision arc
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 prevPoint = eyePosition + leftBoundary;
            int segments = 20;
            for (int i = 1; i <= segments; i++)
            {
                float angle = -halfAngle + (halfAngle * 2f * i / segments);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward * _stats.visionDistance;
                Vector3 point = eyePosition + direction;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            // Draw eye position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePosition, 0.1f);

            // Draw height range (上下对称)
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f); // Transparent cyan
            Vector3 topPosition = eyePosition + Vector3.up * _stats.visionHeightRange;
            Vector3 bottomPosition = eyePosition - Vector3.up * _stats.visionHeightRange;
            
            // Draw horizontal lines at top and bottom of height range
            float radius = _stats.visionDistance;
            int circleSegments = 16;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle1 = (i / (float)circleSegments) * 360f;
                float angle2 = ((i + 1) / (float)circleSegments) * 360f;
                
                Vector3 dir1 = Quaternion.Euler(0f, angle1, 0f) * Vector3.forward * radius;
                Vector3 dir2 = Quaternion.Euler(0f, angle2, 0f) * Vector3.forward * radius;
                
                // Top circle
                Gizmos.DrawLine(topPosition + new Vector3(dir1.x, 0f, dir1.z), 
                                topPosition + new Vector3(dir2.x, 0f, dir2.z));
                
                // Bottom circle
                Gizmos.DrawLine(bottomPosition + new Vector3(dir1.x, 0f, dir1.z), 
                                bottomPosition + new Vector3(dir2.x, 0f, dir2.z));
            }
            
            // Draw vertical lines connecting top and bottom at vision cone boundaries
            Vector3 leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
            Vector3 rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;
            Gizmos.DrawLine(topPosition + new Vector3(leftDir.x, 0f, leftDir.z) * radius,
                           bottomPosition + new Vector3(leftDir.x, 0f, leftDir.z) * radius);
            Gizmos.DrawLine(topPosition + new Vector3(rightDir.x, 0f, rightDir.z) * radius,
                           bottomPosition + new Vector3(rightDir.x, 0f, rightDir.z) * radius);

            // Draw last known position if available
            if (_hasLastKnownPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_lastKnownPlayerPosition, 0.3f);
                Gizmos.DrawLine(eyePosition, _lastKnownPlayerPosition);
            }

            // Draw raycasts to player if visible
            if (_playerTransform != null)
            {
                Vector3 playerPosition = _playerTransform.position;
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                
                foreach (Vector3 checkPointOffset in _playerCheckPoints)
                {
                    Vector3 targetPoint = playerPosition + checkPointOffset;
                    Gizmos.DrawLine(eyePosition, targetPoint);
                    Gizmos.DrawWireSphere(targetPoint, 0.1f);
                }
            }
        }

        #endregion
    }
}

