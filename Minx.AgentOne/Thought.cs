
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using System.Text;

namespace Minx.AgentOne
{
    public class Thought
    {
        public string Internal { get; set; }
        public IList<ToolCall> ToolCalls { get; set; }

        public string Contextualize()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<Thought>");

            if (!string.IsNullOrEmpty(Internal))
            {
                sb.AppendLine($"    <Internal>{Internal}</Internal>");
            }

            if (ToolCalls != null && ToolCalls.Count > 0)
            {
                sb.AppendLine("    <ToolCalls>");
                foreach (var toolCall in ToolCalls)
                {
                    sb.AppendLine($"        <ToolCall>");
                    sb.AppendLine($"            <Name>{toolCall.FunctionCall.Name}</Name>");
                    sb.AppendLine($"            <Arguments>{toolCall.FunctionCall.Arguments}</Arguments>");
                    sb.AppendLine($"        </ToolCall>");
                }
                sb.AppendLine("    </ToolCalls>");
            }

            sb.AppendLine("</Thought>");

            return sb.ToString();
        }
    }
}