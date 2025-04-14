using Minx.AgentOne;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI;
using Minx.ZMesh;

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

int port = NamedArguments.GetAs("Port", 10001);
string name = NamedArguments.GetAs("Name", "AgentOne");

var sysmap = SystemMap.LoadFile("systemmap.yaml");
var zmesh = new ZMesh("localhost:" + port, sysmap);

var brain = new Brain(openAiService);
var shortTermMemory = new ShortTermMemory();

var character = new AgentCharacter
{
    Name = name
};

var agent = new Agent(brain, shortTermMemory, character);

agent.Actuators.Add(new MessageBoxActuator(zmesh, character));
agent.Sensors.Add(new MessageBoxSensor(zmesh.At(name)));

var cancellationTokenSource = new CancellationTokenSource();

Console.WriteLine($"This is {name}");

await Task.Run(() => agent.ExecuteAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
