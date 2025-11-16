using UnityEngine;
using Resonance.Systems.Waves;

namespace Resonance.Presentations.Waves
{
    /// <summary>
    /// Wave renderer for presentation layer
    /// Handles rendering Wave data to LineRenderer
    /// Separates rendering logic from UI logic
    /// </summary>
    public static class WaveRenderer
    {
        /// <summary>
        /// Render configuration
        /// </summary>
        public struct RenderConfig
        {
            public RectTransform renderArea;
            public float scrollOffset;
            public bool useWorldSpace;
            public float lineWidth;
            
            public static RenderConfig Default => new RenderConfig
            {
                scrollOffset = 0f,
                useWorldSpace = true,
                lineWidth = 0.002f
            };
        }
        
        /// <summary>
        /// Render a wave to a LineRenderer with scrolling effect
        /// </summary>
        public static void RenderScrolling(Wave wave, LineRenderer lineRenderer, RectTransform renderArea, float scrollOffset)
        {
            if (wave == null || lineRenderer == null || renderArea == null)
            {
                Debug.LogWarning("WaveRenderer: Cannot render - wave, lineRenderer, or renderArea is null");
                return;
            }
            
            RenderConfig config = RenderConfig.Default;
            config.renderArea = renderArea;
            config.scrollOffset = scrollOffset;
            
            Render(wave, lineRenderer, config);
        }
        
        /// <summary>
        /// Render a wave to a LineRenderer as static (no scrolling)
        /// </summary>
        public static void RenderStatic(Wave wave, LineRenderer lineRenderer, RectTransform renderArea)
        {
            if (wave == null || lineRenderer == null || renderArea == null)
            {
                Debug.LogWarning("WaveRenderer: Cannot render - wave, lineRenderer, or renderArea is null");
                return;
            }
            
            RenderConfig config = RenderConfig.Default;
            config.renderArea = renderArea;
            config.scrollOffset = 0f;
            
            Render(wave, lineRenderer, config);
        }
        
        /// <summary>
        /// Core rendering method
        /// </summary>
        public static void Render(Wave wave, LineRenderer lineRenderer, RenderConfig config)
        {
            if (wave == null || lineRenderer == null)
            {
                Debug.LogWarning("WaveRenderer: Cannot render - wave or lineRenderer is null");
                return;
            }
            
            // Get render area world corners
            Vector3[] corners = new Vector3[4];
            if (config.renderArea != null)
            {
                config.renderArea.GetWorldCorners(corners);
            }
            else
            {
                Debug.LogWarning("WaveRenderer: Render area is null, using default bounds");
                corners[0] = new Vector3(-1f, -1f, 0f); // Bottom-left
                corners[1] = new Vector3(-1f, 1f, 0f);  // Top-left
                corners[3] = new Vector3(1f, -1f, 0f);  // Bottom-right
            }
            
            // Calculate world space dimensions
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);
            Vector3 bottomLeftOrigin = corners[0];
            float worldZ = bottomLeftOrigin.z;
            
            // Get waveform resolution
            int waveformCount = wave.Resolution;
            
            // Configure LineRenderer
            lineRenderer.useWorldSpace = config.useWorldSpace;
            lineRenderer.startWidth = config.lineWidth;
            lineRenderer.endWidth = config.lineWidth;
            lineRenderer.positionCount = waveformCount;
            lineRenderer.enabled = true;
            
            // Sample and set positions
            for (int i = 0; i < waveformCount; i++)
            {
                float t = (float)i / (waveformCount - 1); // Normalized position [0, 1]
                float x = bottomLeftOrigin.x + t * worldWidth;
                
                // Calculate wave value with scroll offset
                float wavePosition = (t + config.scrollOffset) % 1f;
                float waveValueRaw = wave.GetWaveValue(wavePosition);
                
                // Normalize to [0, 1] for Y position
                float normalizedY = 0.5f;
                if (wave.Amplitude > 0f)
                {
                    normalizedY = (waveValueRaw / wave.Amplitude + 1f) * 0.5f;
                }
                
                // Clamp to valid range
                normalizedY = Mathf.Clamp01(normalizedY);
                
                // Calculate Y position
                float y = bottomLeftOrigin.y + normalizedY * worldHeight;
                
                // Set position
                Vector3 position = new Vector3(x, y, worldZ);
                lineRenderer.SetPosition(i, position);
            }
        }
        
        /// <summary>
        /// Render multiple waves to a single LineRenderer (for superposition visualization)
        /// </summary>
        public static void RenderMultiple(Wave[] waves, LineRenderer lineRenderer, RectTransform renderArea, float scrollOffset = 0f)
        {
            if (waves == null || waves.Length == 0 || lineRenderer == null || renderArea == null)
            {
                Debug.LogWarning("WaveRenderer: Cannot render multiple waves - invalid input");
                return;
            }
            
            // Get render area world corners
            Vector3[] corners = new Vector3[4];
            renderArea.GetWorldCorners(corners);
            
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);
            Vector3 bottomLeftOrigin = corners[0];
            float worldZ = bottomLeftOrigin.z;
            
            // Use the first wave's resolution
            int waveformCount = waves[0].Resolution;
            
            // Configure LineRenderer
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = waveformCount;
            lineRenderer.enabled = true;
            
            // Sample and combine waves
            for (int i = 0; i < waveformCount; i++)
            {
                float t = (float)i / (waveformCount - 1);
                float x = bottomLeftOrigin.x + t * worldWidth;
                
                // Sum all wave values
                float combinedValue = 0f;
                float maxAmplitude = 0f;
                
                foreach (Wave wave in waves)
                {
                    if (wave == null) continue;
                    
                    float wavePosition = (t + scrollOffset) % 1f;
                    combinedValue += wave.GetWaveValue(wavePosition);
                    maxAmplitude = Mathf.Max(maxAmplitude, wave.Amplitude);
                }
                
                // Normalize combined value
                float normalizedY = 0.5f;
                if (maxAmplitude > 0f)
                {
                    normalizedY = (combinedValue / maxAmplitude + 1f) * 0.5f;
                }
                
                normalizedY = Mathf.Clamp01(normalizedY);
                float y = bottomLeftOrigin.y + normalizedY * worldHeight;
                
                Vector3 position = new Vector3(x, y, worldZ);
                lineRenderer.SetPosition(i, position);
            }
        }
    }
}

