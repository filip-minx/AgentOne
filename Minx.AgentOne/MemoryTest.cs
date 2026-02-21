using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Interfaces;

namespace Minx.AgentOne
{
    public class MemoryTest
    {
        public static async Task RunAsync(IOpenAIService? openAiService = null)
        {
            Console.WriteLine("=== Memory System Test ===\n");

            ILongTermMemory longTermMemory;
            if (openAiService != null)
            {
                Console.WriteLine("Using real OpenAI embeddings\n");
                longTermMemory = new EmbeddingLongTermMemory(openAiService);
            }
            else
            {
                Console.WriteLine("Using mock embeddings (no API key required)\n");
                longTermMemory = new MockEmbeddingLongTermMemory();
            }

            // Create some test sensory data with different importance levels
            var testMemories = new List<(string content, float importance, string description)>
            {
                ("My name is Alice and I love programming.", 0.9f, "High importance - introduction"),
                ("I am working on an AI agent project.", 0.8f, "High importance - goal/objective"),
                ("The weather is nice today.", 0.2f, "Low importance - small talk"),
                ("Remember that my favorite color is blue.", 0.95f, "Very high importance - explicit remember"),
                ("Hello there!", 0.1f, "Low importance - greeting"),
                ("I need help with debugging my code.", 0.7f, "Medium importance - request"),
                ("Ok, thanks!", 0.15f, "Low importance - acknowledgment"),
                ("My project deadline is next Friday.", 0.85f, "High importance - important fact"),
                ("Just chatting.", 0.2f, "Low importance - filler"),
                ("I prefer Python over JavaScript for backend work.", 0.75f, "Medium-high importance - preference")
            };

            // Store memories
            Console.WriteLine("Storing memories...\n");
            foreach (var (content, importance, description) in testMemories)
            {
                var data = new TestSensoryData(content);
                await longTermMemory.RememberAsync(data, importance);
                Console.WriteLine($"  [{importance:F2}] {description}");
                Console.WriteLine($"       \"{content}\"\n");
            }

            Console.WriteLine($"\nTotal memories stored: {longTermMemory.Count}\n");
            Console.WriteLine(new string('=', 60));

            // Test retrieval with different queries
            var queries = new List<(string query, string reasoning)>
            {
                ("What is your name?", "Should recall the introduction"),
                ("Tell me about your project", "Should recall project-related memories"),
                ("What are your preferences?", "Should recall preferences about color and programming"),
                ("What's your deadline?", "Should recall the deadline information"),
                ("Talk about the weather", "Should recall weather-related memory")
            };

            foreach (var (query, reasoning) in queries)
            {
                Console.WriteLine($"\n\nQuery: \"{query}\"");
                Console.WriteLine($"Expected: {reasoning}\n");

                var relevant = await longTermMemory.RecallRelevantAsync(query, limit: 3);

                if (relevant.Count > 0)
                {
                    Console.WriteLine("Retrieved memories:");
                    for (int i = 0; i < relevant.Count; i++)
                    {
                        Console.WriteLine($"  {i + 1}. \"{relevant[i].Recall}\"");
                    }
                }
                else
                {
                    Console.WriteLine("  No relevant memories found");
                }

                Console.WriteLine(new string('-', 60));
            }

            Console.WriteLine("\n\n=== Memory Test Complete ===");
        }

        private class TestSensoryData : SensoryData
        {
            private readonly string content;

            public TestSensoryData(string content)
            {
                this.content = content;
                Sensor = new DummySensor();
                Thought = new Thought
                {
                    Internal = "Test thought",
                    ToolCalls = Array.Empty<Betalgo.Ranul.OpenAI.ObjectModels.RequestModels.ToolCall>()
                };
            }

            public override string ProcessingInstructions => content;
            public override string Recall => content;

            private class DummySensor : ISensor
            {
                public string Description => "Test sensor";
                public bool TryGetData(out SensoryData data)
                {
                    data = null!;
                    return false;
                }
            }
        }
    }
}
