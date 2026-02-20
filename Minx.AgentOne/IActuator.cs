
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public interface IActuator
    {
        string Description { get; }
        List<ToolDefinition> GetToolDefinitions();
        public Task ExecuteAsync(string functionName, Dictionary<string, string> parameters);
    }
}
