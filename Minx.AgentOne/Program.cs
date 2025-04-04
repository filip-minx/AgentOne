using Minx.AgentOne;

var brain = new Brain();
var agent = new Agent(brain);

var messageBox = new MessageBoxSensor();

agent.Sensors.Add(messageBox);


var cancellationTokenSource = new CancellationTokenSource();
_ = Task.Run(() => agent.ExecuteAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);

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