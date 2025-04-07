using Newtonsoft.Json;
using OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public class Agent : IAgent
    {
        public Agent(IBrain brain)
        {
            Brain = brain;
        }
        public IBrain Brain { get; }
        public List<ISensor> Sensors { get; } = new List<ISensor>();

        public List<IActuator> Actuators { get; } = new List<IActuator>();

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await TickAsync(cancellationToken);

                // Wait for a short period before the next iteration
                await Task.Delay(100); // Adjust the sleep duration as needed
            }
        }

        private async Task TickAsync(CancellationToken cancellationToken)
        {
            // Execute the agent's logic here
            // For example, read sensor data and perform actions
            foreach (var sensor in Sensors)
            {
                if (sensor.TryGetData(out var data))
                {
                    var work = await Brain.Think(data, Actuators, Sensors);

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
