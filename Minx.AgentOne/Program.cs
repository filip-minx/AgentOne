using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Interfaces;
using Betalgo.Ranul.OpenAI.Managers;
using Minx.AgentOne;
using Minx.ZMesh;

// Check for test mode
if (args.Length > 0 && args[0] == "--test-memory")
{
    await TestRunner.Main(args);
    return;
}

string lmStudioBaseDomain = "https://api.openai.com";
//string lmStudioBaseDomain = "http://localhost:1234";

var httpClient = new HttpClient()
{
    Timeout = TimeSpan.FromMinutes(5)
};

var openAiService = new OpenAIService(new OpenAIOptions()
{
    BaseDomain = lmStudioBaseDomain,
    ApiKey = "..."
}, httpClient);

int port = NamedArguments.GetAs("Port", 10001);
string name = NamedArguments.GetAs("Name", "AgentOne");

var sysmap = SystemMap.LoadFile("systemmap.yaml");
var zmesh = new ZMesh("localhost:" + port, sysmap);

var brain = new Brain(openAiService);
var shortTermMemory = new ShortTermMemory();
var longTermMemory = new EmbeddingLongTermMemory(openAiService);

var character = new AgentCharacter
{
    Name = name
};

var agent = new Agent(brain, shortTermMemory, longTermMemory, character);

agent.Actuators.Add(new MessageBoxActuator(zmesh, character));
agent.Sensors.Add(new MessageBoxSensor(zmesh.At(name)));

var cancellationTokenSource = new CancellationTokenSource();

Console.WriteLine($"This is {name}");

await Task.Run(() => agent.ExecuteAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
