namespace Resonance.Core
{
    public interface IState
    {
        string Name { get; }
        void Enter();
        void Update();
        void Exit();
        bool CanTransitionTo(IState newState);
    }
}
