namespace Minx.AgentOne
{
    public interface ILongTermMemory
    {
        /// <summary>
        /// Store a memory with importance score for later retrieval
        /// </summary>
        Task RememberAsync(SensoryData data, float importance);

        /// <summary>
        /// Retrieve memories relevant to the given query using semantic similarity
        /// </summary>
        Task<List<SensoryData>> RecallRelevantAsync(string query, int limit = 5);

        /// <summary>
        /// Get all stored memories
        /// </summary>
        Task<List<SensoryData>> RecallAllAsync();

        /// <summary>
        /// Get count of stored memories
        /// </summary>
        int Count { get; }
    }
}
