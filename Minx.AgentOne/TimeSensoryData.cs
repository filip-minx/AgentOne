namespace Minx.AgentOne
{
    public class TimeSensoryData : SensoryData
    {
        public DateTime CurrentTime { get; }
        public TimeSpan ElapsedSinceLastTick { get; }

        public TimeSensoryData(DateTime currentTime, TimeSpan elapsedSinceLastTick, ISensor sensor)
        {
            CurrentTime = currentTime;
            ElapsedSinceLastTick = elapsedSinceLastTick;
            Sensor = sensor;
        }

        public override string ProcessingInstructions =>
            $"Time update: Current UTC time is {CurrentTime:yyyy-MM-dd HH:mm:ss}. " +
            (ElapsedSinceLastTick.TotalSeconds > 0
                ? $"{ElapsedSinceLastTick.TotalSeconds:F0} seconds have elapsed since your last time awareness update."
                : "This is your first time awareness update.");

        public override string Recall =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] Time check";

        public override string ToString() => ProcessingInstructions;
    }
}
