namespace Minx.AgentOne
{
    public interface IBrain
    {
        Work Think(SensoryData data);
    }

    internal class Brain : IBrain
    {
        public Work Think(SensoryData data)
        {
            Console.WriteLine(data.ProcessingInstructions);
            return new Work();
        }
    }
}