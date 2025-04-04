namespace Minx.AgentOne
{
    public class Work
    {

    }

    

    public abstract class SensoryData
    {
        public abstract string ProcessingInstructions { get; }

        public abstract string TextData { get; }
    }

    public class MessageBoxSensoryData : SensoryData
    {
        public MessageBoxSensoryData(Message message)
        {
            Message = message;
        }

        public Message Message { get; set; }

        public override string ProcessingInstructions =>
@$"You sensed new data from the Nessage Box sensor.
You have received a message from an agent named ""{Message.Sender}"".
Content of the message:
{TextData}";

        public override string TextData => Message.Text;
    }

    public class Message
    {
        public string Sender { get; set; }
        public string Text { get; set; }
    }

    public class MessageBoxSensor : ISensor
    {
        private Message message;

        public string ProcessingInstructions { get; set; } = "MessageBoxSensor";

        public bool TryGetData(out SensoryData data)
        {
            data = message != null ? new MessageBoxSensoryData(message) : null;
            message = null; // Reset the message after processing
            return data != null;
        }

        public void AddMessage(Message message)
        {
            // Simulate getting data from a message box
            this.message = message;
        }
    }

    public class Agent : IAgent
    {
        public Agent(IBrain brain)
        {
            Brain = brain;
        }
        public IBrain Brain { get; } = new Brain();
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
                    Brain.Think(data);
                }
            }

            await Task.CompletedTask;
        }
    }
}
