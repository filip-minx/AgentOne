namespace Minx.AgentOne
{
    public class ShortTermMemory : IShortTermMemory
    {
        private Queue<SensoryData> memory = new Queue<SensoryData>();

        public SensoryData Remember(SensoryData data)
        {
            memory.Enqueue(data);

            if (memory.Count > 20) // Limit memory to the last 10 items
            {
                return memory.Dequeue();
            }

            return null;
        }

        public List<SensoryData> Recall()
        {
            return memory.ToList();
        }
    }
}