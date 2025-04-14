using OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public class Thought
    {
        public string Internal { get; set; }
        public IList<ToolCall> ToolCalls { get; set; }
    }
}