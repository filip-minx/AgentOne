using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Newtonsoft.Json;

namespace Minx.AgentOne
{
    /// <summary>
    /// Represents an action that was taken by the agent.
    /// Stored in memory so the agent can recall what it has done.
    /// </summary>
    public class ActionData : Interaction
    {
        public string ActionName { get; }
        public Dictionary<string, string> Parameters { get; }

        public ActionData(string actionName, Dictionary<string, string> parameters)
        {
            ActionName = actionName;
            Parameters = parameters ?? new Dictionary<string, string>();
        }

        public ActionData(ToolCall toolCall)
        {
            ActionName = toolCall.FunctionCall.Name;
            Parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(toolCall.FunctionCall.Arguments)
                         ?? new Dictionary<string, string>();
        }

        public override string ProcessingInstructions =>
            $"You took action: {ActionName}" +
            (Parameters.Any() ? $" with parameters: {string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))}" : "");

        public override string Recall =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] Action: {ActionName}" +
            (Parameters.Any() ? $" ({string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))})" : "");

        public override string ToString() => Recall;
    }
}
