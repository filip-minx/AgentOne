# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minx.AgentOne** is a self-contained reasoning agent built in C# / .NET 8. It implements a sense-think-act loop where the agent:
1. Receives sensory input through pluggable Sensors
2. Reasons about the input using an LLM (Brain)
3. Takes actions through pluggable Actuators

The agent uses OpenAI function calling for tool use and maintains short-term memory of recent interactions. It's designed for building distributed swarms of AI agents that communicate via ZMesh message boxes.

## Design Philosophy

AgentOne implements a dual-intelligence system that combines two fundamental types of intelligence required for autonomous problem-solving:

### Predictive Intelligence (PDI)
What LLMs excel at: extrapolating future states from their trained model, handling ambiguous problems through pattern recognition and intuition. This is the "reasoning" capability of the Brain.

### Agentic Intelligence (ATI)
Using deterministic tools/machines guided by PDI to solve problems that can't be accurately predicted. This addresses a critical limitation: LLMs alone fail at tasks requiring exact computation (like counting Rs in "Strawberry" - you can guess, but not consistently get the right answer).

### Why Both Are Needed

**Pure PDI limitations**: LLMs can simulate simple processes in their "mental model" but fail when:
- Simulation time is too long
- Context is too large
- Deterministic accuracy is required (e.g., mathematical computation)

**The AgentOne solution** achieves true autonomous intelligence by combining both:

1. **Meta-Reasoning (PDI)** - Brain recognizes when a problem needs deterministic computation
2. **Tool Selection (PDI → ATI bridge)** - Brain selects appropriate actuator from catalog
3. **Tool Invocation** - Agent routes the tool call
4. **Deterministic Execution (ATI)** - Actuator performs exact computation
5. **Result Integration** - Outcome stored in memory for future reasoning

**Example**: To count letters in "Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz", the Brain (PDI) recognizes this requires deterministic processing and invokes a tool, rather than attempting to "guess" from its model. The actuator (ATI) executes the exact algorithm and returns the precise count.

This design philosophy explains why the architecture separates Sensors (perceive what's not in the model), Brain (PDI reasoning), Actuators (ATI tools), and Memory (context for reasoning) - it's implementing a system that goes beyond what pure LLMs can accomplish.

## Prerequisites

- **.NET 8 SDK** - Required to build and run the project
- **OpenAI-compatible API endpoint** - Default expects `http://localhost:1234` (e.g., LM Studio) or OpenAI API at `https://api.openai.com`
- **OpenAI API key** - Set in Program.cs line 18 (replace `"..."` with actual key)
- **systemmap.yaml** - Defines agent topology (list of agent addresses in format `hostname:port/AgentName`)
- **ZMesh project** - Messaging library dependency located at `../ZMesh` (sibling directory)

## Build and Run Commands

### Build
```bash
cd Minx.AgentOne
dotnet restore
dotnet build
```

### Run Single Agent
```bash
dotnet run --Port 10001 --Name "AgentOne"
```

### Run Multiple Agents (in separate terminals)
```bash
# Terminal 1
dotnet run --Port 10000 --Name "AgentZero"

# Terminal 2
dotnet run --Port 10001 --Name "AgentOne"

# Terminal 3
dotnet run --Port 10002 --Name "AgentTwo"
```

### Configuration
Create `systemmap.yaml` in the working directory:
```yaml
---
systemMap:
- localhost:10000/AgentZero
- localhost:10001/AgentOne
- localhost:10002/AgentTwo
```

Update `Program.cs` line 7-8 to configure the LLM endpoint:
```csharp
string lmStudioBaseDomain = "http://localhost:1234";  // For LM Studio
// OR
string lmStudioBaseDomain = "https://api.openai.com"; // For OpenAI API
```

Set API key on line 18:
```csharp
ApiKey = "sk-..." // Replace with actual key
```

## Architecture

### Core Loop
The agent runs a tick-based execution loop in `Agent.ExecuteAsync()` that implements the PDI+ATI design:
1. **Sense** - Poll each sensor for new data (perceive environment beyond the model)
2. **Build Working Memory** - Combine short-term memory + semantically relevant long-term memories
3. **Think** - Brain uses PDI to reason about data with full working memory context
4. **Act** - Execute tool calls via actuators (ATI deterministic execution)
5. **Remember** - Store in short-term memory; important memories (score >= 0.5) also go to long-term

### Key Components

**Agent** (`Agent.cs`)
- Orchestrates the sense-think-act loop
- Holds collections of Sensors and Actuators
- Executes tool calls by matching function names to actuators
- Manages short-term memory

**Brain** (`Brain.cs`)
- Implements Predictive Intelligence (PDI) via LLM calls
- Wraps Betalgo.Ranul.OpenAI client for chat completion
- Constructs system prompts with available actuators, sensors, and memory context
- Parses LLM response into Thought (internal reasoning + tool calls)
- Uses OpenAI function calling to bridge PDI reasoning to ATI tool execution

**Sensors** (implement `ISensor`)
- Input sources that perceive the environment (what's not encoded in the LLM model)
- `TryGetData()` returns true when new data is available
- Built-in: `MessageBoxSensor` - receives messages from other agents via ZMesh

**Actuators** (implement `IActuator`)
- Implement Agentic Intelligence (ATI) through deterministic tool execution
- `GetToolDefinitions()` returns OpenAI function definitions for the Brain to select
- `ExecuteAsync()` performs the actual action when LLM calls the tool
- Built-in: `MessageBoxActuator` - sends messages to other agents via ZMesh

**ShortTermMemory** (`ShortTermMemory.cs`)
- Fixed-size queue (200 items) of SensoryData
- Each item includes: sensor data, processing instructions, and the thought it generated
- Oldest memories are forgotten when capacity is exceeded
- Provides recent context for immediate reasoning

**LongTermMemory** (`EmbeddingLongTermMemory.cs`)
- Semantic memory storage using embedding-based retrieval
- Stores important memories (importance score >= 0.5) with vector embeddings
- Retrieval uses cosine similarity between query and stored memories
- Combines similarity score (70%) and importance score (30%) for relevance ranking
- Uses OpenAI's text-embedding-ada-002 model for generating embeddings
- Working memory = short-term + semantically relevant long-term memories

**ZMesh Integration**
- Distributed messaging system (separate project in `../ZMesh`)
- Provides message boxes for inter-agent communication
- Each agent has a named message box at a specific address
- Agents send typed messages to each other via `ZMesh.At(name).Tell(message)`

### Memory Architecture

The agent uses a three-tier memory system:

**Working Memory** (ephemeral, per-tick)
- Combination of short-term memory (recent context) + relevant long-term memories
- Built dynamically for each reasoning cycle by querying long-term memory with current sensory data
- Prevents duplication - only adds long-term memories not already in short-term

**Short-Term Memory** (200 items, FIFO)
- Automatic storage of all recent sensory data + thoughts
- Provides immediate temporal context
- No filtering - everything is remembered until evicted

**Long-Term Memory** (unlimited, semantic)
- Selective storage based on importance score
- Memories with importance >= 0.5 are stored with embeddings
- Retrieval via semantic similarity (cosine distance)
- Relevance = 0.7 × similarity + 0.3 × importance

**Importance Scoring** (calculated by Brain)
- Base: 0.3
- +0.4: Explicit memory requests ("remember"), introductions ("my name is"), goals
- +0.2: Tool calls (actions taken)
- +0.15: Questions or requests
- +0.1: Complex thoughts (>200 chars)
- -0.2: Simple greetings/acknowledgments (<50 chars)
- Clamped to [0.0, 1.0]

### Data Flow

```
MessageBoxSensor.TryGetData()
  → Agent receives Message
    → Agent.GetWorkingMemoryAsync() builds context:
      - Short-term memory (last 200 items)
      - Long-term memory (top 10 semantically relevant)
    → Brain.Think() calls LLM with:
      - Sensory data
      - Available actuators (as tools)
      - Working memory
      → LLM returns Thought (reasoning + tool calls + importance score)
        → Agent stores in short-term memory
        → If importance >= 0.5: Agent stores in long-term memory with embedding
        → Agent.ExecuteWorkAsync() routes tool calls to actuators
          → MessageBoxActuator.ExecuteAsync() sends message via ZMesh
            → Target agent's MessageBoxSensor receives it
```

## Key Dependencies

- **Betalgo.Ranul.OpenAI** v9.0.4 - OpenAI API client (supports function calling, streaming, reasoning effort)
- **Newtonsoft.Json** v13.0.4 - JSON serialization for tool call arguments
- **Microsoft.Extensions.Hosting** v9.0.4 - Hosting infrastructure
- **Minx.ZMesh** (project reference) - Distributed messaging library

## Extending the Agent

### Adding a Custom Sensor

1. Implement `ISensor` interface:
```csharp
public class MySensor : ISensor
{
    public string Description => "Description of what this sensor does";

    public bool TryGetData(out SensoryData data)
    {
        // Check if new data is available
        // If yes: create SensoryData and return true
        // If no: set data = default and return false
    }
}
```

2. Register in `Program.cs`:
```csharp
agent.Sensors.Add(new MySensor());
```

### Adding a Custom Actuator

1. Implement `IActuator` interface:
```csharp
public class MyActuator : IActuator
{
    public string Description => "Description of what this actuator does";

    public List<ToolDefinition> GetToolDefinitions()
    {
        // Define OpenAI functions that the LLM can call
        return new List<ToolDefinition>
        {
            ToolDefinition.DefineFunction(
                new FunctionDefinitionBuilder("MyAction", "Description")
                    .AddParameter("param1", PropertyDefinition.DefineString("Param description"))
                    .Validate()
                    .Build())
        };
    }

    public async Task ExecuteAsync(string functionName, Dictionary<string, string> parameters)
    {
        // Execute the action
        if (functionName == "MyAction")
        {
            // Use parameters["param1"]
        }
    }
}
```

2. Register in `Program.cs`:
```csharp
agent.Actuators.Add(new MyActuator());
```

## Code Patterns

**Agent Initialization Pattern:**
```csharp
var openAiService = new OpenAIService(new OpenAIOptions { ... });
var brain = new Brain(openAiService);
var shortTermMemory = new ShortTermMemory();
var longTermMemory = new EmbeddingLongTermMemory(openAiService);
var character = new AgentCharacter { Name = name };
var agent = new Agent(brain, shortTermMemory, longTermMemory, character);
agent.Sensors.Add(...);
agent.Actuators.Add(...);
await agent.ExecuteAsync(cancellationToken);
```

**Tool Definition Pattern (in Actuators):**
```csharp
ToolDefinition.DefineFunction(
    new FunctionDefinitionBuilder("FunctionName", "Description")
        .AddParameter("paramName", PropertyDefinition.DefineString("param description"))
        .Validate()
        .Build())
```

**Message Passing Pattern (via ZMesh):**
```csharp
// Send
zMesh.At("AgentName").Tell(new Message { Sender = "Me", Text = "Hello" });

// Receive (in sensor)
if (messageBox.TryListen<Message>(m => receivedMessage = m)) { ... }
```

## Important Implementation Details

- The agent loop ticks every 100ms (`Task.Delay(100)` in Agent.cs:30)
- Sensors are polled sequentially on each tick
- Short-term memory is limited to 200 items; oldest items are forgotten when full
- Long-term memory stores only important memories (importance score >= 0.5)
- Working memory is rebuilt each tick from short-term + top 10 relevant long-term memories
- Embeddings are generated using OpenAI's text-embedding-ada-002 model (1536 dimensions)
- Memory retrieval uses cosine similarity with relevance = 0.7 × similarity + 0.3 × importance
- Importance is calculated by Brain using heuristics (keywords, tool calls, message complexity)
- Tool call arguments are deserialized as `Dictionary<string, string>` (Agent.cs:96)
- The Brain includes `<think>` tags in the system prompt to encourage reasoning
- Sensory data includes both processing instructions and a recall string for memory
- The agent logs all sensory data, thoughts, tool calls, and memory operations to console
- Error handling: sensor exceptions are caught and logged without stopping the agent loop
