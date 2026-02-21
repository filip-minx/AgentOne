using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Interfaces;
using Betalgo.Ranul.OpenAI.Managers;

namespace Minx.AgentOne
{
    public class TestRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== AgentOne Memory System Test ===\n");

            try
            {
                // Check for API key - use real embeddings if available, mock otherwise
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                if (!string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("OPENAI_API_KEY detected - using real embeddings");
                    var httpClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromMinutes(5)
                    };

                    var openAiService = new OpenAIService(new OpenAIOptions()
                    {
                        BaseDomain = "https://api.openai.com",
                        ApiKey = apiKey
                    }, httpClient);

                    await MemoryTest.RunAsync(openAiService);
                }
                else
                {
                    Console.WriteLine("OPENAI_API_KEY not set - using mock embeddings");
                    Console.WriteLine("(Set OPENAI_API_KEY environment variable to use real embeddings)\n");
                    await MemoryTest.RunAsync(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest failed with error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
