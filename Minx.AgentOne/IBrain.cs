namespace Minx.AgentOne
{
    public interface IBrain
    {
        Task<Thought> Think(SensoryData data, List<IActuator> availableActuators, List<ISensor> availableSensors, List<Interaction> workingMemory);
    }
}