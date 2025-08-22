namespace Resonance.Core
{
    public enum SystemState
    {
        Uninitialized,
        Initializing,
        Running,
        Shutdown
    }

    public interface IGameSystem
    {
        int Priority { get; }
        SystemState State { get; }
        void Initialize();
        void Shutdown();
    }
}
