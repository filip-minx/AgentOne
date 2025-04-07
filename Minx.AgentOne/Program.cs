using Minx.AgentOne;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI;

string lmStudioBaseDomain = "http://localhost:1234";
var httpClient = new HttpClient()
{
    Timeout = TimeSpan.FromMinutes(5)
};

IChatCompletionService openAiService = new OpenAIService(new OpenAiOptions()
{
    BaseDomain = lmStudioBaseDomain,
    ApiKey = "lm-studio"
}, httpClient);

var brain = new Brain(openAiService);

var agent = new Agent(brain);

agent.Actuators.Add(new MessageBoxActuator());
agent.Sensors.Add(new MessageBoxSensor());

var messageBox = new MessageBoxSensor();

agent.Sensors.Add(messageBox);

var cancellationTokenSource = new CancellationTokenSource();

_ = Task.Run(() => agent.ExecuteAsync(cancellationTokenSource.Token), cancellationTokenSource.Token).ContinueWith(t =>
{
    t.Exception?.Handle(e =>
    {
        Console.WriteLine($"Error: {e.Message}");
        return true;
    });
});

while (true)
{
    Console.WriteLine("Enter a message (or 'exit' to quit):");
    var input = Console.ReadLine();
    if (input?.ToLower() == "exit")
    {
        cancellationTokenSource.Cancel();
        break;
    }

    var message = new Message
    {
        Sender = "Console Agent",
        Text = input
    };

    messageBox.AddMessage(message);
}