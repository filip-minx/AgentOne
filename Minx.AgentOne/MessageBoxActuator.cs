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

        public MessageBoxActuator(IZMesh zMesh)
        {
            this.zMesh = zMesh;
        }
        public string Description => "Allows sending of messages to a message box by its name. You can send messages to other agents.";

        public async Task ExecuteAsync(string functionName, Dictionary<string, string> parameters)
        {
            Console.WriteLine($"Executing function: {functionName}");

            foreach (var parameter in parameters)
            {
                Console.WriteLine($"{parameter.Key}: {parameter.Value}");
            }
            // Simulate sending a message to a message box
            // In a real implementation, you would send the message to the specified message box here.

            zMesh.At(parameters["MessageBoxName"]).Tell(new Message
            {
                Sender = "AgentOne",
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
