namespace Minx.AgentOne
{
    public class MessageBoxSensor : ISensor
    {
        private Message message;

        public string ProcessingInstructions { get; set; } = "MessageBoxSensor";

        public string Description => "You can receive messages to your message box with your name. Other agents can send you messages.";

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
}
