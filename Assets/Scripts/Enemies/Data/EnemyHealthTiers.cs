namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy mental health tier based on percentage thresholds
    /// Affects physical damage taken and dealt
    /// </summary>
    public enum EnemyMentalHealthTier
    {
        Healthy,    // > 40% - normal damage
        Critical,   // > 0%, ≤ 40% - physical damage taken and dealt * 1.5
        Dead        // = 0% - physical damage taken and dealt * 2.0, true death
    }

    /// <summary>
    /// Enemy physical health tier based on percentage thresholds
    /// Affects movement speed and combat capabilities
    /// </summary>
    public enum EnemyPhysicalHealthTier
    {
        Healthy,    // > 40% - normal speed and abilities
        Wounded,    // > 0%, ≤ 40% - reduced movement speed
        Dead        // = 0% - cannot move/attack, core exposed, start revival
    }
}
