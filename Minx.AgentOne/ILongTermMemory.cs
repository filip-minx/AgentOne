namespace Minx.AgentOne
{
    public interface ILongTermMemory
    {
        /// <summary>
        /// Store an interaction (sensory data or action) with importance score for later retrieval
        /// </summary>
        Task RememberAsync(Interaction interaction, float importance);

        /// <summary>
        /// Retrieve interactions relevant to the given query using semantic similarity
        /// </summary>
        Task<List<Interaction>> RecallRelevantAsync(string query, int limit = 5);

        /// <summary>
        /// Get all stored interactions
        /// </summary>
        Task<List<Interaction>> RecallAllAsync();

        /// <summary>
        /// Get count of stored interactions
        /// </summary>
        int Count { get; }
    }
}
