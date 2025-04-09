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

var zmesh = new ZMesh("localhost:10001", SystemMap.LoadFile("systemmap.yaml"));

var brain = new Brain(openAiService);

var agentName = "AgentOne";
var agent = new Agent(brain);

agent.Actuators.Add(new MessageBoxActuator());
agent.Sensors.Add(new MessageBoxSensor(zmesh.At(agentName)));

var cancellationTokenSource = new CancellationTokenSource();

await Task.Run(() => agent.ExecuteAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
