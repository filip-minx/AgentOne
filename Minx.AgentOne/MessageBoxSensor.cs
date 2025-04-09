using Minx.ZMesh;

namespace Minx.AgentOne
{
    public class MessageBoxSensor : ISensor
    {
        private readonly ITypedMessageBox messageBox;

        public string ProcessingInstructions { get; set; } = "MessageBoxSensor";

        public string Description => "You can receive messages to your message box with your name. Other agents can send you messages.";

        public MessageBoxSensor(ITypedMessageBox messageBox)
        {
            this.messageBox = messageBox;
        }

        public bool TryGetData(out SensoryData data)
        {
            Message message = null;
            
            if (messageBox.TryListen<Message>(m => message = m))
            {
                data = new MessageBoxSensoryData(message);
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }
    }
}
