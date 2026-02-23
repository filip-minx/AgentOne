# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minx.AgentOne** is a self-contained reasoning agent built in C# / .NET 8. It implements a sense-think-act loop where the agent:
1. Receives sensory input through pluggable Sensors (including time awareness)
2. Reasons about the input using an LLM (Brain)
3. Takes actions through pluggable Actuators
4. Maintains complete memory of all sensory data and actions taken

The agent uses OpenAI function calling for tool use, has temporal awareness for time-based reasoning, and stores complete history of all experiences and actions. It's designed for building distributed swarms of AI agents that communicate via ZMesh message boxes and can take autonomous actions based on elapsed time and memory.

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
1. **Sense** - Poll each sensor for new data (messages, time updates, etc.)
2. **Build Working Memory** - Combine short-term memory + semantically relevant long-term memories from complete history
3. **Think** - Brain uses PDI to reason about data with full working memory and temporal context
4. **Act** - Execute tool calls via actuators (ATI deterministic execution)
5. **Remember** - Store ALL sensory data AND all actions taken as separate, explicit entries in both short-term and long-term memory (importance score preserved for retrieval ranking)

### Key Components

**Interaction** (`Interaction.cs`) - Base class for all agent experiences
- Abstract base class for everything stored in memory
- Two main branches:
  - `SensoryData` - input TO the agent (from sensors)
    - `MessageBoxSensoryData` - messages received
    - `TimeSensoryData` - time updates
  - `ActionData` - output FROM the agent (actions taken)
    - `MessageBoxActionData` - messages sent
- **Symmetry**: Sensors create specific SensoryData types, Actuators create specific ActionData types
- Properties:
  - `Timestamp` - when this interaction occurred (UTC) - **all interactions are timestamped**
  - `ProcessingInstructions` - how the Brain should process this
  - `Recall` - concise string for memory display (includes timestamp)
  - `Thought` - the thought generated in response to this interaction

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
- `TryGetData()` returns SensoryData when new data is available
- Built-in sensors:
  - `MessageBoxSensor` - receives messages from other agents via ZMesh
  - `TimeSensor` - provides periodic time updates for temporal awareness and autonomous action (default: every 30 seconds)

**Actuators** (implement `IActuator`)
- Implement Agentic Intelligence (ATI) through deterministic tool execution
- `GetToolDefinitions()` returns OpenAI function definitions for the Brain to select
- `ExecuteAsync()` performs the actual action and returns specific ActionData describing what was done
- **Actuators create their own ActionData types** (similar to sensors creating SensoryData types)
- Built-in: `MessageBoxActuator` - sends messages and creates `MessageBoxActionData`

**ShortTermMemory** (`ShortTermMemory.cs`)
- Fixed-size queue (200 items) of Interactions (both SensoryData and ActionData)
- Each item includes: processing instructions, recall text, and the thought it generated
- Oldest interactions are forgotten when capacity is exceeded
- Provides recent context of both sensory input and actions taken for immediate reasoning

**LongTermMemory** (`EmbeddingLongTermMemory.cs`)
- Complete history storage - ALL interactions (SensoryData + ActionData) stored with vector embeddings
- Interactions are properly typed:
  - `SensoryData` (inherits from Interaction) - input from sensors
  - `ActionData` (inherits from Interaction) - actions taken with name, parameters, timestamp
- Semantic retrieval using cosine similarity between query and stored interactions
- Combines similarity score (70%) and importance score (30%) for relevance ranking
- Actions have default importance of 0.7 (high priority for recall)
- Enables temporal reasoning by providing access to complete history of events and actions
- Uses OpenAI's text-embedding-ada-002 model for generating embeddings
- Working memory = short-term + semantically relevant long-term interactions

**ZMesh Integration**
- Distributed messaging system (separate project in `../ZMesh`)
- Provides message boxes for inter-agent communication
- Each agent has a named message box at a specific address
- Agents send typed messages to each other via `ZMesh.At(name).Tell(message)`

### Temporal Awareness and Autonomous Action

The agent has built-in temporal awareness through the `TimeSensor`, which enables:

**Time-Based Reasoning:**
- Tracks current time and elapsed time since last update
- Can reason about timing of past events from complete memory history
- Understands temporal patterns (e.g., "I asked a question 1 minute ago and haven't received a response")

**Autonomous Action:**
- Agent can act without waiting for external messages
- Makes decisions based on time passing and memory of past events
- Examples:
  - Following up on unanswered questions after reasonable time
  - Taking periodic actions based on goals
  - Proactive status checks or reminders

**Configuration:**
- TimeSensor fires every 30 seconds by default (configurable in Program.cs)
- Time updates are stored in long-term memory with base importance of 0.4
- Temporal reasoning (elapsed time, waiting, follow-up) increases importance by 0.15

### Memory Architecture

The agent uses a three-tier memory system:

**Working Memory** (ephemeral, per-tick)
- Combination of short-term memory (recent context) + relevant long-term memories
- Built dynamically for each reasoning cycle by querying long-term memory with current sensory data
- Prevents duplication - only adds long-term memories not already in short-term

**Short-Term Memory** (200 items, FIFO)
- Automatic storage of all recent sensory data + thoughts + actions taken
- Each action is stored as a separate explicit `ActionSensoryData` entry
- Provides immediate temporal context including action history
- No filtering - everything is remembered until evicted

**Long-Term Memory** (unlimited, semantic, complete history)
- **ALL sensory data and actions are stored as explicit separate entries** (complete audit trail)
- Actions stored with `ActionSensoryData` containing action name, parameters, and timestamp
- Each memory includes importance score for relevance ranking during recall
- Actions have default importance of 0.7 (high priority for recall)
- Retrieval via semantic similarity (cosine distance)
- Relevance = 0.7 × similarity + 0.3 × importance
- Enables temporal reasoning by providing complete history of past events and actions with exact timing

**Importance Scoring** (calculated by Brain)
- Base: 0.3
- +0.4: Explicit memory requests ("remember"), introductions ("my name is"), goals
- +0.3: Tool calls (actions taken) - ensures ALL actions are stored with high importance
- +0.15: Questions or requests
- +0.15: Temporal reasoning (elapsed time, waiting, follow-up decisions)
- +0.1: Complex thoughts (>200 chars)
- Base 0.4: Time sensor updates (temporal awareness)
- -0.2: Simple greetings/acknowledgments (<50 chars)
- Clamped to [0.0, 1.0]
- **Note**: All memories are now stored regardless of importance score; score affects retrieval relevance ranking

### Data Flow

```
Sensors.TryGetData() [MessageBoxSensor, TimeSensor, etc.]
  → Agent receives sensory data (messages, time updates, etc.)
    → Agent.GetWorkingMemoryAsync() builds context:
      - Short-term memory (last 200 items: sensory data + actions)
      - Long-term memory (top 10 semantically relevant from complete history)
    → Brain.Think() calls LLM with:
      - Sensory data
      - Available actuators (as tools)
      - Working memory (includes past actions taken)
      - Temporal awareness context
      → LLM returns Thought (reasoning + tool calls + importance score)
        → Agent stores sensory data in short-term memory
        → Agent stores sensory data in long-term memory (ALL data, with importance for ranking)
        → Agent.ExecuteWorkAsync() routes tool calls to actuators
          → Actuators execute actions (send messages, etc.)
            → Each action is stored explicitly in memory:
              - Creates ActionSensoryData with action name, parameters, timestamp
              - Stores in short-term memory (importance: 0.7)
              - Stores in long-term memory (importance: 0.7)
            → Target agent's sensors receive results

Time-based autonomous action flow:
TimeSensor fires → Agent becomes aware of elapsed time → Brain reasons about past events and actions
  → Decides to take action (e.g., follow up on unanswered question)
    → Executes tool calls autonomously
      → Actions stored in memory with timestamps
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

1. Create a specific ActionData type for your actuator:
```csharp
public class MyActionData : ActionData
{
    public string MyParameter { get; }

    public MyActionData(string myParameter)
        : base("my_action", new Dictionary<string, string> { { "param1", myParameter } })
    {
        MyParameter = myParameter;
    }

    public override string ProcessingInstructions =>
        $"You performed my_action with parameter: {MyParameter}";

    public override string Recall =>
        $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] My Action: {MyParameter}";
}
```

2. Implement `IActuator` interface:
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

    public async Task<ActionData> ExecuteAsync(string functionName, Dictionary<string, string> parameters)
    {
        // Execute the action
        if (functionName == "MyAction")
        {
            var param1 = parameters["param1"];
            // Do the actual work...

            // Return specific ActionData
            return new MyActionData(param1);
        }

        throw new InvalidOperationException($"Unknown function: {functionName}");
    }
}
```

3. Register in `Program.cs`:
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

- The agent loop ticks every 100ms (`Task.Delay(100)` in Agent.cs:32)
- Sensors are polled sequentially on each tick (MessageBoxSensor, TimeSensor, custom sensors)
- **TimeSensor fires every 30 seconds** (configurable), enabling time-based reasoning and autonomous action
- Short-term memory is limited to 200 items; oldest items are forgotten when full
- **Long-term memory stores ALL sensory data and actions** (complete history with embeddings)
- Working memory is rebuilt each tick from short-term + top 10 semantically relevant long-term memories
- Embeddings are generated using OpenAI's text-embedding-ada-002 model (1536 dimensions)
- Memory retrieval uses cosine similarity with relevance = 0.7 × similarity + 0.3 × importance
- Importance is calculated by Brain using heuristics:
  - Base 0.3, +0.4 for explicit memory/goals, +0.3 for tool calls, +0.15 for questions/temporal reasoning
  - Time sensor data has base importance of 0.4
  - Score is preserved for retrieval ranking but all memories are stored
- Tool call arguments are deserialized as `Dictionary<string, string>` (Agent.cs:134)
- The Brain includes `<think>` tags in the system prompt to encourage reasoning
- The Brain's system prompt emphasizes temporal awareness and autonomous action capabilities
- Sensory data includes both processing instructions and a recall string for memory
- The agent logs all sensory data, thoughts, tool calls, and memory operations to console
- Error handling: sensor exceptions are caught and logged without stopping the agent loop
