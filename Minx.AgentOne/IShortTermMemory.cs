namespace Minx.AgentOne
{
    public interface IShortTermMemory
    {
        public SensoryData Remember(SensoryData data);

        public List<SensoryData> Recall();
    }
}