namespace Minx.AgentOne
{
    /// <summary>
    /// Base class for all agent interactions - both sensory input and actions taken.
    /// Represents a single unit of experience in the agent's memory.
    /// </summary>
    public abstract class Interaction
    {
        /// <summary>
        /// When this interaction occurred (UTC).
        /// </summary>
        public DateTime Timestamp { get; protected set; }

        /// <summary>
        /// Instructions for how the Brain should process this interaction.
        /// </summary>
        public abstract string ProcessingInstructions { get; }

        /// <summary>
        /// A concise string representation for memory recall.
        /// </summary>
        public abstract string Recall { get; }

        /// <summary>
        /// The thought generated in response to this interaction (if any).
        /// </summary>
        public Thought? Thought { get; set; }

        protected Interaction()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
