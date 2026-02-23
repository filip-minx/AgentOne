namespace Minx.AgentOne
{
    /// <summary>
    /// Represents sensory input received from a sensor.
    /// This is input TO the agent from the environment.
    /// </summary>
    public abstract class SensoryData : Interaction
    {
        public ISensor Sensor { get; set; }
    }
}
