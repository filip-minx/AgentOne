
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public interface IActuator
    {
        string Description { get; }
        List<ToolDefinition> GetToolDefinitions();

        /// <summary>
        /// Execute an action and return the ActionData describing what was done.
        /// The actuator is responsible for creating the appropriate ActionData subclass.
        /// </summary>
        public Task<ActionData> ExecuteAsync(string functionName, Dictionary<string, string> parameters);
    }
}
