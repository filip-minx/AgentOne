using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using System.Text;

namespace Minx.AgentOne
{
    internal class Brain : IBrain
    {
        private IChatCompletionService chatCompletionService;

        public Brain(IChatCompletionService chatCompletionService)
        {
            this.chatCompletionService = chatCompletionService;
        }

        public async Task<Thought> Think(SensoryData data, List<IActuator> availableActuators, List<ISensor> availableSensors, List<SensoryData> shortTermMemory)
        {
            Console.WriteLine("Thinking about the sensory data...");

            var tools = availableActuators.Select(actuator => actuator.GetToolDefinitions()).SelectMany(x => x).ToList();

            var systemPrompt =
$@"You are a highly capable reasoning AI agent with integrated tool use functionality.
When you receive a query, first analyze whether external tools (such as APIs or calculation functions) are needed to generate a complete and accurate response.
If so, generate a tool call using the predefined JSON schema that specifies the function name and parameters.
Always include a dedicated internal reasoning section between `<think>` and `</think>` tags before and after any tool invocation. Ensure your response remains clear,
logically structured, and concise. If no tool is required, proceed with your internal reasoning and respond directly.

Your available actuators are within the <Actuators></Actuators> XML tags.
<Actuators>
{GetActuatorsInstructions(availableActuators)}
</Actuators>

Your available sensors are within the <Sensors></Sensors> XML tags.
<Sensors>
{GetSensorsInstructions(availableSensors)}
</Sensors>

The history of your sensory data is within the <Memory></Memory> XML tags. Think about the previous sensory data carefully. Base your next decision on the previous sensory data.
<Memory>
{GetShortTermMemoryInstructions(shortTermMemory)}
</Memory>
";
            var completionResult = await chatCompletionService.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(systemPrompt),
                    ChatMessage.FromUser(data.ProcessingInstructions)
                },
                // Model name often doesn't matter much for LM Studio if only one model is loaded,
                // but it's good practice to include it. You might need to match what LM Studio expects.
                Model = "local-model", // Or the specific model identifier if needed/known
                Tools = tools,
                Stream = false
            });

            var calls = completionResult.Choices.First().Message.ToolCalls;

            return new Thought
            {
                Internal = completionResult.Successful
                    ? completionResult.Choices.First().Message.Content
                    : "Error in thought process. " + completionResult.Error,
                ToolCalls = calls ?? Array.Empty<ToolCall>()
            };
        }

        private string GetActuatorsInstructions(List<IActuator> availableActuators)
        {
            var sb = new StringBuilder();

            foreach (var actuator in availableActuators)
            {
                sb.AppendLine($"- {actuator.GetType().Name}: {actuator.Description}");
                foreach (var tool in actuator.GetToolDefinitions())
                {
                    sb.AppendLine($"  - {tool.Function.Name}: {tool.Function.Description}");
                    foreach (var parameter in tool.Function.Parameters.Properties)
                    {
                        sb.AppendLine($"    - {parameter.Key}: {parameter.Value.Description}");
                    }
                }
            }

            return sb.ToString();
        }

        private string GetSensorsInstructions(List<ISensor> availableSensors)
        {
            var sb = new StringBuilder();

            foreach (var sensor in availableSensors)
            {
                sb.AppendLine($"- {sensor.GetType().Name}: {sensor.Description}");
            }
            return sb.ToString();
        }

        private string GetShortTermMemoryInstructions(List<SensoryData> shortTermMemory)
        {
            var sb = new StringBuilder();

            foreach (var item in shortTermMemory)
            {
                sb.AppendLine(item.Recall);
            }

            return sb.ToString();
        }
    }
}