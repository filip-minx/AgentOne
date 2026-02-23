namespace Minx.AgentOne
{
    public class TimeSensor : ISensor
    {
        private DateTime? lastTick;
        private readonly TimeSpan tickInterval;

        public string Description => "Provides periodic time updates to maintain temporal awareness. Helps track elapsed time and enables time-based reasoning.";

        public TimeSensor(TimeSpan tickInterval)
        {
            this.tickInterval = tickInterval;
        }

        public bool TryGetData(out SensoryData data)
        {
            var now = DateTime.UtcNow;

            // Fire on first call or after interval has elapsed
            if (!lastTick.HasValue || (now - lastTick.Value) >= tickInterval)
            {
                var elapsedSinceLastTick = lastTick.HasValue ? now - lastTick.Value : TimeSpan.Zero;
                lastTick = now;

                data = new TimeSensoryData(now, elapsedSinceLastTick, this);
                return true;
            }

            data = default!;
            return false;
        }
    }
}
