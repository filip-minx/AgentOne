using Newtonsoft.Json;
using OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public class Agent : IAgent
    {
        private readonly IShortTermMemory shortTermMemory;
        private readonly AgentCharacter character;

        public Agent(IBrain brain, IShortTermMemory shortTermMemory, AgentCharacter character)
        {
            Brain = brain;

            this.shortTermMemory = shortTermMemory;
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
                if (sensor.TryGetData(out var data))
                {
                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine($"Sensor {data.Sensor.GetType().Name} received data.");
                    Console.WriteLine("Data: " + data.ToString());

                    var thought = await Brain.Think(data, Actuators, Sensors, shortTermMemory.Recall());

                    LogThought(thought);

                    var forgottenMemory = shortTermMemory.Remember(data);
                    if (forgottenMemory != null)
                    {
                        Console.WriteLine("Short term memory is full. Forgetting the oldest memory.");
                    }

                    await ExecuteWorkAsync(thought.ToolCalls);

                    Console.WriteLine("------------------------------------------");
                }
            }

            await Task.CompletedTask;
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
                    await actuator.ExecuteAsync(toolCall.FunctionCall.Name, parameters);
                }
            }

            await Task.CompletedTask;
        }
    }
}
