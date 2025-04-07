namespace Minx.AgentOne
{
    public interface ISensor
    {
        public string Description { get; }

        /// <summary>
        /// Gets unprocessed sensory data since the last call to this function.
        /// </summary>
        /// <returns></returns>
        bool TryGetData(out SensoryData data);
    }
}
