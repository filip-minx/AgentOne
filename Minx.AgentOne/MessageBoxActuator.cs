using Minx.ZMesh;
using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

namespace Minx.AgentOne
{
    public class MessageBoxActuator : IActuator
    {
        ToolDefinition SendMessageFunction = ToolDefinition.DefineFunction(new FunctionDefinitionBuilder("SendMessage", "Sends a message to a specified MessageBox via the MessageBoxActuator.")
            .AddParameter("MessageBoxName", PropertyDefinition.DefineString("The name of the target message box. E.g. \"Agent Smith\". The name is case sensitive and can contain whitespace."))
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

        public async Task ExecuteAsync(string functionName, Dictionary<string, string> parameters)
        {
            zMesh.At(parameters["MessageBoxName"]).Tell(new Message
            {
                Sender = agentCharacter.Name,
                Text = parameters["MessageContent"]
            });
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
