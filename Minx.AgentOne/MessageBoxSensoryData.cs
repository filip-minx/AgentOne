namespace Minx.AgentOne
{
    public class MessageBoxSensoryData : SensoryData
    {
        public MessageBoxSensoryData(Message message)
        {
            Message = message;
        }

        public Message Message { get; set; }

        public override string ProcessingInstructions =>
@$"You sensed new data from the Message Box sensor.
You have received a message from an agent named ""{Message.Sender}"".
The content of the message is within the <MessageContent></MessageContent> XML tags.
<MessageContent>{TextData}</MessageContent>

Think about the message carefully. How do you want to react to it?";
        public override string TextData => Message.Text;
    }
}
