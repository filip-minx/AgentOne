namespace Minx.AgentOne
{
    public class ShortTermMemory : IShortTermMemory
    {
        private Stack<SensoryData> memory = new Stack<SensoryData>();

        public void Remember(SensoryData data)
        {
            memory.Push(data);

            if (memory.Count > 20) // Limit memory to the last 10 items
            {
                memory.Pop();
            }
        }

        public List<SensoryData> Recall()
        {
            return memory.ToList();
        }
    }
}