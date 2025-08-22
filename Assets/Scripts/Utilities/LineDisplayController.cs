using UnityEngine;
using System.Collections;

namespace Resonance.Utilities
{
    /// <summary>
    /// 简单的控制器，用于管理LineRenderer的临时显示
    /// </summary>
    public class LineDisplayController : MonoBehaviour
    {
        /// <summary>
        /// 临时显示线条
        /// </summary>
        /// <param name="lineRenderer">要控制的LineRenderer</param>
        /// <param name="duration">显示时长</param>
        public void ShowLineTemporarily(LineRenderer lineRenderer, float duration)
        {
            StartCoroutine(HideLineAfterDelay(lineRenderer, duration));
        }

        private IEnumerator HideLineAfterDelay(LineRenderer lineRenderer, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
