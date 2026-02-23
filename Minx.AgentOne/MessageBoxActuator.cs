using Betalgo.Ranul.OpenAI.Builders;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Betalgo.Ranul.OpenAI.ObjectModels.SharedModels;
using Minx.ZMesh;

namespace Minx.AgentOne
{
    public class MessageBoxActuator : IActuator
    {
        ToolDefinition SendMessageFunction = ToolDefinition.DefineFunction(new FunctionDefinitionBuilder("SendMessage", "Sends a message to a specified MessageBox via the MessageBoxActuator.")
            .AddParameter("MessageBoxName", PropertyDefinition.DefineString("The name of the target message box. E.g. \"Agent Smith\". The name is case sensitive and whitespace sensitive."))
            .AddParameter("MessageContent", PropertyDefinition.DefineString("Content of the sent message."))
            .Validate()
        .Build());

        private IZMesh zMesh;
        private readonly AgentCharacter agentCharacter;

        public MessageBoxActuator(IZMesh zMesh, AgentCharacter agentCharacter)
        {
            this.zMesh = zMesh;
            this.agentCharacter = agentCharacter;
        }

        public string Description => "Allows sending of messages to a message box by its name. You can send messages to other agents.";

        public async Task<ActionData> ExecuteAsync(string functionName, Dictionary<string, string> parameters)
        {
            var recipient = parameters["MessageBoxName"];
            var messageContent = parameters["MessageContent"];

            // Execute the action
            zMesh.At(recipient).Tell(new Message
            {
                Sender = agentCharacter.Name,
                Text = messageContent
            });

            // Create and return the specific ActionData for this actuator
            return new MessageBoxActionData(recipient, messageContent);
        }

        public List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                SendMessageFunction
            };
        }


    }
}
