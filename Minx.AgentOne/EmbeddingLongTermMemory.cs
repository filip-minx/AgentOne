using Betalgo.Ranul.OpenAI.Interfaces;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

namespace Minx.AgentOne
{
    public class EmbeddingLongTermMemory : ILongTermMemory
    {
        private readonly IOpenAIService openAiService;
        private readonly List<MemoryEntry> memories = new();
        private readonly object lockObject = new();

        public EmbeddingLongTermMemory(IOpenAIService openAiService)
        {
            this.openAiService = openAiService;
        }

        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return memories.Count;
                }
            }
        }

        public async Task RememberAsync(Interaction interaction, float importance)
        {
            // Generate embedding for the memory's recall text
            var embedding = await GetEmbeddingAsync(interaction.Recall);

            var entry = new MemoryEntry
            {
                Interaction = interaction,
                Embedding = embedding,
                Importance = importance,
                Timestamp = DateTime.UtcNow
            };

            lock (lockObject)
            {
                memories.Add(entry);
            }

            Console.WriteLine($"[LTM] Stored memory (importance: {importance:F2}): {TruncateText(interaction.Recall, 80)}");
        }

        public async Task<List<Interaction>> RecallRelevantAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Interaction>();
            }

            List<MemoryEntry> snapshot;
            lock (lockObject)
            {
                if (memories.Count == 0)
                {
                    return new List<Interaction>();
                }
                snapshot = new List<MemoryEntry>(memories);
            }

            // Generate embedding for the query
            var queryEmbedding = await GetEmbeddingAsync(query);

            // Calculate similarity scores and rank memories
            var rankedMemories = snapshot
                .Select(m => new
                {
                    Memory = m,
                    Similarity = CosineSimilarity(queryEmbedding, m.Embedding),
                    // Combine similarity with importance for relevance score
                    Relevance = CosineSimilarity(queryEmbedding, m.Embedding) * 0.7f + m.Importance * 0.3f
                })
                .OrderByDescending(x => x.Relevance)
                .Take(limit)
                .ToList();

            Console.WriteLine($"[LTM] Recalled {rankedMemories.Count} relevant memories for query: {TruncateText(query, 60)}");
            foreach (var item in rankedMemories)
            {
                Console.WriteLine($"  - Relevance: {item.Relevance:F3} (sim: {item.Similarity:F3}, imp: {item.Memory.Importance:F2}) - {TruncateText(item.Memory.Interaction.Recall, 60)}");
            }

            return rankedMemories.Select(x => x.Memory.Interaction).ToList();
        }

        public Task<List<Interaction>> RecallAllAsync()
        {
            lock (lockObject)
            {
                return Task.FromResult(memories.Select(m => m.Interaction).ToList());
            }
        }

        private async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                // Return zero vector for empty text
                return new float[1536]; // Default OpenAI embedding size
            }

            var request = new EmbeddingCreateRequest
            {
                Model = "text-embedding-ada-002", // or "text-embedding-3-small"
                Input = text
            };

            var result = await openAiService.Embeddings.CreateEmbedding(request);

            if (!result.Successful)
            {
                Console.WriteLine($"[LTM] Embedding generation failed: {result.Error?.Message}");
                throw new Exception($"Failed to generate embedding: {result.Error?.Message}");
            }

            return result.Data.First().Embedding.Select(d => (float)d).ToArray();
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentException("Vectors must have the same length");
            }

            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            magnitudeA = MathF.Sqrt(magnitudeA);
            magnitudeB = MathF.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 0;
            }

            return dotProduct / (magnitudeA * magnitudeB);
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength - 3) + "...";
        }

        private class MemoryEntry
        {
            public Interaction Interaction { get; set; }
            public float[] Embedding { get; set; }
            public float Importance { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
