namespace Minx.AgentOne
{
    /// <summary>
    /// Represents a message sent action taken by the MessageBoxActuator.
    /// This is the output counterpart to MessageBoxSensoryData.
    /// </summary>
    public class MessageBoxActionData : ActionData
    {
        public string Recipient { get; }
        public string MessageText { get; }

        public MessageBoxActionData(string recipient, string messageText)
            : base("send_message", new Dictionary<string, string>
            {
                { "recipient", recipient },
                { "message", messageText }
            })
        {
            Recipient = recipient;
            MessageText = messageText;
        }

        public override string ProcessingInstructions =>
            $"You sent a message to {Recipient}: \"{MessageText}\"";

        public override string Recall =>
            $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] Sent message to {Recipient}: \"{MessageText}\"";

        public override string ToString() => Recall;
    }
}
