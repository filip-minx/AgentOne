using OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public interface IBrain
    {
        Task<IList<ToolCall>> Think(SensoryData data, List<IActuator> availableActuators, List<ISensor> availableSensors, List<SensoryData> shortTermMemory);
    }
}