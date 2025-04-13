using Newtonsoft.Json;
using OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public class Agent : IAgent
    {
        private readonly IShortTermMemory shortTermMemory;

        public Agent(IBrain brain, IShortTermMemory shortTermMemory)
        {
            Brain = brain;
            this.shortTermMemory = shortTermMemory;
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
                    var work = await Brain.Think(data, Actuators, Sensors, shortTermMemory.Recall());
                    shortTermMemory.Remember(data);
                    await ExecuteWorkAsync(work);
                }
            }

            await Task.CompletedTask;
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
