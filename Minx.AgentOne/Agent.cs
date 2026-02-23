using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Newtonsoft.Json;

namespace Minx.AgentOne
{
    public class Agent : IAgent
    {
        private readonly IShortTermMemory shortTermMemory;
        private readonly ILongTermMemory longTermMemory;
        private readonly AgentCharacter character;

        public Agent(IBrain brain, IShortTermMemory shortTermMemory, ILongTermMemory longTermMemory, AgentCharacter character)
        {
            Brain = brain;

            this.shortTermMemory = shortTermMemory;
            this.longTermMemory = longTermMemory;
            this.character = character;
        }

        public IBrain Brain { get; }
        public List<ISensor> Sensors { get; } = new List<ISensor>();

        public List<IActuator> Actuators { get; } = new List<IActuator>();

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await TickAsync(cancellationToken);

                await Task.Delay(100);
            }
        }

        private async Task TickAsync(CancellationToken cancellationToken)
        {
            foreach (var sensor in Sensors)
            {
                try
                {
                    if (sensor.TryGetData(out var data))
                    {
                        Console.WriteLine("------------------------------------------");
                        Console.WriteLine($"Sensor {data.Sensor.GetType().Name} received data.");
                        Console.WriteLine("Data: " + data.ToString());

                        // Build working memory: short-term + relevant long-term
                        var workingMemory = await GetWorkingMemoryAsync(data);

                        var thought = await Brain.Think(data, Actuators, Sensors, workingMemory);

                        data.Thought = thought;

                        LogThought(thought);

                        var forgottenMemory = shortTermMemory.Remember(data);
                        if (forgottenMemory != null)
                        {
                            Console.WriteLine("[STM] Short term memory full. Forgetting oldest interaction.");
                        }

                        // Store ALL sensory data in long-term memory
                        // The importance score is preserved for relevance ranking during recall
                        await longTermMemory.RememberAsync(data, thought.ImportanceScore);
                        Console.WriteLine($"[Memory] Stored sensory input in long-term memory (importance: {thought.ImportanceScore:F2})");

                        // Execute actions and store them explicitly in memory
                        await ExecuteWorkAsync(thought.ToolCalls);

                        Console.WriteLine("------------------------------------------");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in sensor {sensor.GetType().Name}: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }

        private async Task<List<Interaction>> GetWorkingMemoryAsync(SensoryData currentData)
        {
            var working = new List<Interaction>();

            // Add recent context from short-term memory (includes both sensory data and actions)
            working.AddRange(shortTermMemory.Recall());

            // Add relevant interactions from long-term memory
            if (longTermMemory.Count > 0)
            {
                var relevantMemories = await longTermMemory.RecallRelevantAsync(
                    currentData.ProcessingInstructions,
                    limit: 10);

                // Add long-term memories that aren't already in short-term
                foreach (var ltm in relevantMemories)
                {
                    if (!working.Contains(ltm))
                    {
                        working.Add(ltm);
                    }
                }
            }

            return working;
        }

        private void LogThought(Thought thought)
        {
            if (!string.IsNullOrEmpty(thought.Internal))
            {
                Console.WriteLine("Thought: " + thought.Internal);
            }
            
            if (thought.ToolCalls.Any())
            {
                foreach (var toolCall in thought.ToolCalls)
                {
                    Console.WriteLine($"Tool Call: {toolCall.FunctionCall.Name} with arguments {toolCall.FunctionCall.Arguments}");
                }
            }
        }

        private async Task ExecuteWorkAsync(IList<ToolCall> work)
        {
            foreach (var toolCall in work)
            {
                var actuator = Actuators.FirstOrDefault(a => a.GetToolDefinitions().Any(t => t.Function.Name == toolCall.FunctionCall.Name));

                if (actuator != null)
                {
                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(toolCall.FunctionCall.Arguments);

                    // Execute the action and get the ActionData from the actuator
                    var actionData = await actuator.ExecuteAsync(toolCall.FunctionCall.Name, parameters);

                    // Store the action in memory
                    await StoreActionInMemoryAsync(actionData);
                }
            }

            await Task.CompletedTask;
        }

        private async Task StoreActionInMemoryAsync(ActionData actionData)
        {
            // Create a simple thought for the action (no additional reasoning needed)
            var actionThought = new Thought
            {
                Internal = $"Executed action: {actionData.ActionName}",
                ToolCalls = Array.Empty<ToolCall>(),
                ImportanceScore = 0.7f // Actions are always important (increased from default 0.5)
            };

            actionData.Thought = actionThought;

            // Store in short-term memory
            var forgotten = shortTermMemory.Remember(actionData);
            if (forgotten != null)
            {
                Console.WriteLine("[STM] Short term memory full while storing action. Forgot oldest interaction.");
            }

            // Store in long-term memory
            await longTermMemory.RememberAsync(actionData, actionThought.ImportanceScore);
            Console.WriteLine($"[Memory] Stored action '{actionData.ActionName}' in memory (importance: {actionThought.ImportanceScore:F2})");
        }
    }
}
