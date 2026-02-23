namespace Minx.AgentOne
{
    public interface IShortTermMemory
    {
        public Interaction? Remember(Interaction interaction);

        public List<Interaction> Recall();
    }
}