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
You have received a message from an agent. The agent's name is within the <Sender></Sender> XML tags.
<Sender>{Message.Sender}</Sender>.
The content of the message is within the <MessageContent></MessageContent> XML tags.
<MessageContent>{TextData}</MessageContent>

Think about the message carefully. Only respond to the Sender when the MessageContent is a question or if you are instructed to do so. Otherwise do not respond to the Sender.
How do you want to react?";

        public override string TextData => Message.Text;
    }
}
