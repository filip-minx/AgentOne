namespace Minx.AgentOne
{
    public class MessageBoxSensoryData : SensoryData
    {
        public MessageBoxSensoryData(Message message, ISensor sensor)
        {
            Message = message;
            Sensor = sensor;
        }

        public Message Message { get; set; }

        public override string ProcessingInstructions =>
@$"You sensed new data from the Message Box sensor.
You have received a message from an agent. The agent's name is within the <Sender></Sender> XML tags.
<Sender>{Message.Sender}</Sender>.
The content of the message is within the <MessageContent></MessageContent> XML tags.
<MessageContent>{Message.Text}</MessageContent>

Think about the message carefully. Only respond to the Sender when the MessageContent is a question or if you are now or previously instructed to do so.

How do you want to react?";

        public override string Recall =>
$@"<Message>
    <Sender>{Message.Sender}</Sender>
    <MessageContent>{Message.Text}</MessageContent>
    {Thought.Contextualize()}
</Message>";

        public override string ToString()
        {
            return $"Message from {Message.Sender}: {Message.Text}";
        }
    }
}
