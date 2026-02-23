namespace Minx.AgentOne
{
    public class ShortTermMemory : IShortTermMemory
    {
        private Queue<Interaction> memory = new Queue<Interaction>();

        public Interaction? Remember(Interaction interaction)
        {
            memory.Enqueue(interaction);

            if (memory.Count > 200) // Limit memory to the last 200 items
            {
                return memory.Dequeue();
            }

            return null;
        }

        public List<Interaction> Recall()
        {
            return memory.ToList();
        }
    }
}