using Microsoft.VisualBasic;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using static OpenAI.ObjectModels.SharedModels.IOpenAiModels;
using System.Diagnostics;
using System;
using System.Text;

namespace Minx.AgentOne
{
    public interface IBrain
    {
        

        Task<IList<ToolCall>> Think(SensoryData data, List<IActuator> availableActuators, List<ISensor> availableSensors);
    }

    internal class Brain : IBrain
    {
        private IChatCompletionService chatCompletionService;

        public Brain(IChatCompletionService chatCompletionService)
        {
            this.chatCompletionService = chatCompletionService;
        }

        public List<ISensor> Sensors { get; } = new List<ISensor>();

        public List<IActuator> Actuators { get; } = new List<IActuator>();

        public async Task<IList<ToolCall>> Think(SensoryData data, List<IActuator> availableActuators, List<ISensor> availableSensors)
        {
            var tools = availableActuators.Select(actuator => actuator.GetToolDefinitions()).SelectMany(x => x).ToList();

            var completionResult = await chatCompletionService.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(
$@"You are a highly capable reasoning AI agent with integrated tool use functionality.
When you receive a query, first analyze whether external tools (such as APIs or calculation functions) are needed to generate a complete and accurate response.
If so, generate a tool call using the predefined JSON schema that specifies the function name and parameters.
Always include a dedicated internal reasoning section between `<think>` and `</think>` tags before and after any tool invocation. Ensure your response remains clear,
logically structured, and concise. If no tool is required, proceed with your internal reasoning and respond directly.

Your available actuators are:
{GetActuatorsInstructions()}

Your available sensors are:
{GetSensorsInstructions()}
"),
                    ChatMessage.FromUser(data.ProcessingInstructions)
                },
                // Model name often doesn't matter much for LM Studio if only one model is loaded,
                // but it's good practice to include it. You might need to match what LM Studio expects.
                Model = "local-model", // Or the specific model identifier if needed/known
                Temperature = 0.7f,
                Tools = tools,
                Stream = false
            });

            if (completionResult.Successful)
            {
                Console.WriteLine($"AI: {completionResult.Choices.First().Message.Content}");
            }
            else
            {
                Console.WriteLine("Error calling LM Studio.");
                if (completionResult.Error != null)
                {
                    Console.WriteLine($"Error: {completionResult.Error.Message} (Code: {completionResult.Error.Code})");
                }
            }

            var calls = completionResult.Choices.First().Message.ToolCalls;
            return calls ?? Array.Empty<ToolCall>();
        }

        private string GetActuatorsInstructions()
        {
            var sb = new StringBuilder();

            foreach (var actuator in Actuators)
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

        private string GetSensorsInstructions()
        {
            var sb = new StringBuilder();

            foreach (var sensor in Sensors)
            {
                sb.AppendLine($"- {sensor.GetType().Name}: {sensor.Description}");
            }
            return sb.ToString();
        }
    }
}