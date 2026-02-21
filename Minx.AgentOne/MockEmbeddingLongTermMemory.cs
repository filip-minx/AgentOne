namespace Minx.AgentOne
{
    /// <summary>
    /// Mock implementation of long-term memory that uses simple hashing instead of real embeddings
    /// Useful for testing without requiring OpenAI API calls
    /// </summary>
    public class MockEmbeddingLongTermMemory : ILongTermMemory
    {
        private readonly List<MemoryEntry> memories = new();
        private readonly object lockObject = new();

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

        public Task RememberAsync(SensoryData data, float importance)
        {
            var embedding = GenerateMockEmbedding(data.Recall);

            var entry = new MemoryEntry
            {
                Data = data,
                Embedding = embedding,
                Importance = importance,
                Timestamp = DateTime.UtcNow
            };

            lock (lockObject)
            {
                memories.Add(entry);
            }

            Console.WriteLine($"[LTM] Stored memory (importance: {importance:F2}): {TruncateText(data.Recall, 80)}");
            return Task.CompletedTask;
        }

        public Task<List<SensoryData>> RecallRelevantAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(new List<SensoryData>());
            }

            List<MemoryEntry> snapshot;
            lock (lockObject)
            {
                if (memories.Count == 0)
                {
                    return Task.FromResult(new List<SensoryData>());
                }
                snapshot = new List<MemoryEntry>(memories);
            }

            var queryEmbedding = GenerateMockEmbedding(query);

            // Calculate similarity scores and rank memories
            var rankedMemories = snapshot
                .Select(m => new
                {
                    Memory = m,
                    Similarity = CosineSimilarity(queryEmbedding, m.Embedding),
                    Relevance = CosineSimilarity(queryEmbedding, m.Embedding) * 0.7f + m.Importance * 0.3f
                })
                .OrderByDescending(x => x.Relevance)
                .Take(limit)
                .ToList();

            Console.WriteLine($"[LTM] Recalled {rankedMemories.Count} relevant memories for query: {TruncateText(query, 60)}");
            foreach (var item in rankedMemories)
            {
                Console.WriteLine($"  - Relevance: {item.Relevance:F3} (sim: {item.Similarity:F3}, imp: {item.Memory.Importance:F2}) - {TruncateText(item.Memory.Data.Recall, 60)}");
            }

            return Task.FromResult(rankedMemories.Select(x => x.Memory.Data).ToList());
        }

        public Task<List<SensoryData>> RecallAllAsync()
        {
            lock (lockObject)
            {
                return Task.FromResult(memories.Select(m => m.Data).ToList());
            }
        }

        /// <summary>
        /// Generate a mock embedding using word-based features
        /// This simulates semantic similarity without requiring an API
        /// </summary>
        private float[] GenerateMockEmbedding(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new float[100];
            }

            var embedding = new float[100];
            var words = text.ToLowerInvariant()
                .Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            // Simple feature extraction
            var wordSet = new HashSet<string>(words);

            // Feature 0-9: Common word presence
            var commonWords = new[] { "name", "project", "help", "remember", "color", "prefer", "deadline", "weather", "love", "need" };
            for (int i = 0; i < commonWords.Length; i++)
            {
                embedding[i] = wordSet.Contains(commonWords[i]) ? 1.0f : 0.0f;
            }

            // Feature 10-19: Topic indicators
            if (wordSet.Overlaps(new[] { "programming", "code", "debug", "python", "javascript", "project", "ai" }))
                embedding[10] = 1.0f;
            if (wordSet.Overlaps(new[] { "name", "alice", "bob", "i am", "my name" }))
                embedding[11] = 1.0f;
            if (wordSet.Overlaps(new[] { "color", "blue", "red", "green", "favorite" }))
                embedding[12] = 1.0f;
            if (wordSet.Overlaps(new[] { "deadline", "friday", "monday", "next", "week" }))
                embedding[13] = 1.0f;
            if (wordSet.Overlaps(new[] { "weather", "rain", "sunny", "nice", "today" }))
                embedding[14] = 1.0f;
            if (wordSet.Overlaps(new[] { "help", "need", "request", "please", "can you" }))
                embedding[15] = 1.0f;
            if (wordSet.Overlaps(new[] { "prefer", "like", "love", "favorite", "enjoy" }))
                embedding[16] = 1.0f;

            // Feature 20-99: Word hashing for additional signal
            foreach (var word in words)
            {
                var hash = Math.Abs(word.GetHashCode()) % 80;
                embedding[20 + hash] += 0.1f;
            }

            // Normalize
            var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= magnitude;
                }
            }

            return embedding;
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
            public SensoryData Data { get; set; } = null!;
            public float[] Embedding { get; set; } = null!;
            public float Importance { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
