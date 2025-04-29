namespace Minx.AgentOne
{
    public abstract class SensoryData
    {
        public ISensor Sensor { get; set; }

        public abstract string ProcessingInstructions { get; }

        public abstract string Recall { get; }

        public Thought Thought { get; set; }
    }
}
