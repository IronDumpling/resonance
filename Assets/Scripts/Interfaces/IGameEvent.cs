namespace Resonance.Core
{
    public interface IGameEvent
    {
        float Timestamp { get; }
    }

    public abstract class BaseGameEvent : IGameEvent
    {
        public float Timestamp { get; private set; }

        protected BaseGameEvent()
        {
            Timestamp = UnityEngine.Time.time;
        }
    }
}
