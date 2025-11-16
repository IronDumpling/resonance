using Resonance.Utilities.Waves;

namespace Resonance.Utilities.Waves.WavePhenomenons
{
    /// <summary>
    /// Interface for wave phenomena calculations
    /// Each phenomenon implements this interface for consistent API
    /// </summary>
    public interface IWavePhenomenon
    {
        /// <summary>
        /// Calculate the phenomenon result
        /// </summary>
        /// <param name="context">Wave interaction context</param>
        /// <returns>Phenomenon result</returns>
        WavePhenomenonResult Calculate(WaveInteractionContext context);
        
        /// <summary>
        /// Get the phenomenon type
        /// </summary>
        WavePhenomenonType PhenomenonType { get; }
        
        /// <summary>
        /// Check if this phenomenon can be applied to the given context
        /// </summary>
        bool CanApply(WaveInteractionContext context);
    }
}

