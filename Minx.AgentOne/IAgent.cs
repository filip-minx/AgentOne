namespace Minx.AgentOne
{
    internal interface IAgent
    {
        public Task ExecuteAsync(CancellationToken cancellationToken);

        public List<ISensor> Sensors { get; }

        public List<IActuator> Actuators { get; }
    }
}
