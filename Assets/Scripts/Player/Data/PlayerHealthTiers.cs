namespace Resonance.Player.Data
{
    /// <summary>
    /// Mental health tier based on slot consumption
    /// Mental health is divided into N slots (currently 3)
    /// </summary>
    public enum MentalHealthTier
    {
        High,   // > 1 slot
        Low,    // ≤ 1 slot, > 0  
        Empty   // = 0
    }

    /// <summary>
    /// Physical health tier based on percentage thresholds
    /// Used for UI display and movement speed modifiers
    /// </summary>
    public enum PhysicalHealthTier
    {
        Healthy,    // > 70% (normal_health sprite)
        Wounded,    // > 30%, ≤ 70% (median_health sprite)
        Critical    // ≥ 0%, ≤ 30% (bad_health sprite)
    }
}
