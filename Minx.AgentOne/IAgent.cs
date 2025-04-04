namespace Minx.AgentOne
{
    internal interface IAgent
    {
        List<ISensor> Sensors { get; }

        List<IActuator> Actuators { get; }

        public Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
