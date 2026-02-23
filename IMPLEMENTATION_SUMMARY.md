# Temporal Awareness and Complete Memory Implementation

## Summary

AgentOne has been enhanced with temporal awareness and complete memory capabilities. The agent can now:
1. **Track time** and reason about elapsed time between events
2. **Remember everything** - all sensory input and all actions taken (stored as explicit separate memory entries)
3. **Recall past actions** with exact details (action name, parameters, timestamp)
4. **Take autonomous actions** without waiting for external messages
5. **Make time-based decisions** (e.g., follow up on unanswered questions)

## Architecture: Interaction-Based Memory System

### Core Design

The memory system uses a clean separation between input (sensory data) and output (actions):

```
Interaction (abstract base class)
├── Properties: Timestamp, ProcessingInstructions, Recall, Thought
├── SensoryData (input FROM sensors TO agent)
│   ├── MessageBoxSensoryData (created by MessageBoxSensor)
│   ├── TimeSensoryData (created by TimeSensor)
│   └── (custom sensory data types)
└── ActionData (output FROM agent - actions taken)
    ├── MessageBoxActionData (created by MessageBoxActuator)
    └── (custom action data types)
```

**Why this design?**
- Sensory data and actions are fundamentally different types of interactions
- SensoryData is obtained FROM sensors (input)
- ActionData represents what the agent DOES (output)
- Both are stored in the same memory system as "Interactions"
- **All interactions have timestamps** - consistency across all memory entries
- **Symmetrical pattern**:
  - Sensors create specific SensoryData types
  - Actuators create specific ActionData types
  - Agent doesn't need to know the specifics, just stores Interaction objects
- Memory interfaces (`IShortTermMemory`, `ILongTermMemory`) work with `Interaction` base type

**Timestamp Consistency:**
- Previously: Only ActionData had timestamps, SensoryData did not
- Now: `Timestamp` is in the `Interaction` base class
- All interactions (sensory input and actions) are automatically timestamped on creation
- Enables accurate temporal reasoning about both inputs and outputs

## Changes Made

### 1. New Base Classes

**Interaction.cs** (NEW)
- Abstract base class for all agent experiences
- Properties:
  - `Timestamp` - when this interaction occurred (UTC) - **all interactions are timestamped**
  - `ProcessingInstructions` - how the Brain should process this interaction
  - `Recall` - concise string for memory display (includes timestamp)
  - `Thought` - the thought generated in response to this interaction
- Enables unified memory storage of both inputs and outputs with consistent timestamping

**ActionData.cs** (NEW - replaces incorrect ActionSensoryData)
- Base class for actions taken by the agent
- Inherits from `Interaction` (NOT from SensoryData)
- Contains:
  - Action name (e.g., "send_message")
  - Parameters dictionary (all arguments used)
  - Timestamp inherited from `Interaction` base class
- **Actuators create specific ActionData subclasses** (like MessageBoxActionData)
- Formatted for easy reading in memory with timestamp prefix

**MessageBoxActionData.cs** (NEW)
- Specific ActionData type created by MessageBoxActuator
- Contains recipient and message text as strongly-typed properties
- Provides formatted Recall string: "Sent message to X: Y"
- **Symmetry with MessageBoxSensoryData**: Just as MessageBoxSensor creates MessageBoxSensoryData, MessageBoxActuator creates MessageBoxActionData

**SensoryData.cs** (MODIFIED)
- Now inherits from `Interaction` base class
- Represents input FROM sensors TO the agent
- Retains `Sensor` property to track which sensor provided the data
- Timestamp inherited from `Interaction` base class - all sensory input is timestamped
- Concrete implementations (MessageBoxSensoryData, TimeSensoryData) include timestamp in Recall string

**TimeSensor.cs** (NEW)
- Provides periodic time updates to the agent
- Configurable tick interval (default: 30 seconds)
- Returns `TimeSensoryData` with current UTC time and elapsed time

**TimeSensoryData.cs** (NEW)
- Data model for time sensor information
- Inherits from `SensoryData`
- Enables temporal reasoning and autonomous action

### 2. Modified Actuators

**IActuator** (MODIFIED)
- `ExecuteAsync()` now returns `Task<ActionData>` instead of `Task`
- Actuators are responsible for creating their specific ActionData types
- Parallel pattern to sensors creating SensoryData types

**MessageBoxActuator** (MODIFIED)
- Now creates and returns `MessageBoxActionData` after executing the action
- Extracts recipient and message from parameters
- Returns strongly-typed action data with all context

### 3. Modified Memory System

**IShortTermMemory** and **ILongTermMemory** (MODIFIED)
- Changed from working with `SensoryData` to `Interaction`
- Methods now accept/return `Interaction` instead of `SensoryData`
- Enables storage of both sensory input and actions taken

**ShortTermMemory.cs** (MODIFIED)
- Queue now stores `Interaction` objects
- Can contain both `SensoryData` (inputs) and `ActionData` (outputs)
- Agent's working memory includes both what it experienced and what it did

**EmbeddingLongTermMemory.cs** (MODIFIED)
- Stores `Interaction` objects with embeddings
- MemoryEntry now has `Interaction` property instead of `Data`
- Complete audit trail of all inputs and outputs

**MockEmbeddingLongTermMemory.cs** (MODIFIED)
- Updated to match interface changes
- Uses `Interaction` instead of `SensoryData`

### 4. Modified Agent and Brain

**Agent.cs** (Multiple changes)
- `ExecuteWorkAsync()` now receives ActionData from actuator.ExecuteAsync()
- **Agent no longer creates ActionData** - actuators do this
- Agent simply stores the ActionData returned by actuators
- `StoreActionInMemoryAsync()` accepts ActionData parameter (not ToolCall)
- `GetWorkingMemoryAsync()` now returns `List<Interaction>`
- After executing each action:
  1. Creates an `ActionData` entry (not ActionSensoryData)
  2. Stores it in short-term memory
  3. Stores it in long-term memory with importance 0.7
- Removed `InternalSensor` - no longer needed with proper architecture

**Brain.cs** (MODIFIED)
- `Think()` method now accepts `List<Interaction>` for working memory
- System prompt updated to reference "interactions" instead of "sensory data"
- `GetMemoryInstructions()` works with `Interaction` list
- Brain can reason about both inputs and outputs from memory

**IBrain.cs** (MODIFIED)
- Interface signature updated to use `List<Interaction>` for working memory

### 4. Deleted Files

- **ActionSensoryData.cs** (DELETED) - incorrectly made actions a type of sensory data
- **InternalSensor.cs** (DELETED) - no longer needed with proper ActionData type

## How It Works

### Symmetrical Sensor/Actuator Pattern

The architecture now follows a clean, symmetrical pattern:

**Sensors → SensoryData (Input)**
```csharp
MessageBoxSensor.TryGetData()
  → Creates MessageBoxSensoryData
    → Contains: Message, Sender, Text
      → Agent stores in memory
```

**Actuators → ActionData (Output)**
```csharp
MessageBoxActuator.ExecuteAsync()
  → Performs action (sends message)
    → Creates MessageBoxActionData
      → Contains: Recipient, MessageText
        → Agent stores in memory
```

**Key Insight:**
- Just as sensors know how to format their sensory data, actuators know how to format their action data
- Agent doesn't create ActionData - it receives it from actuators
- Consistent pattern: both sensors and actuators create their own specific Interaction types

### Proper Separation of Concerns

**Before (incorrect):**
```
SensoryData (base)
├── MessageBoxSensoryData
├── TimeSensoryData
└── ActionSensoryData ❌ (actions aren't sensory data!)
```

**After (correct):**
```
Interaction (base)
├── SensoryData (input)
│   ├── MessageBoxSensoryData
│   └── TimeSensoryData
└── ActionData (output) ✓
```

### Memory Storage Flow

```
1. Sensor fires → Returns SensoryData → Stored as Interaction
2. Brain thinks → Decides to act → Tool calls generated
3. Agent executes action → Creates ActionData → Stored as Interaction
4. Working memory = List<Interaction> containing BOTH inputs and outputs
```

### Memory Timeline Example

Your agent's memory now properly separates inputs from outputs:

```
[2026-02-23 14:00:00] Message from AgentTwo: "What's the status?" (SensoryData)
[2026-02-23 14:00:01] Action: send_message (recipient=AgentTwo, ...) (ActionData)
[2026-02-23 14:00:30] Time check (SensoryData)
[2026-02-23 14:01:00] Time check (SensoryData)
[2026-02-23 14:01:15] Action: send_message (recipient=AgentTwo, ...) (ActionData)
```

## Key Benefits

1. **Architecturally Sound**: Proper separation between input (SensoryData) and output (ActionData)
2. **Type Safety**: Memory system works with base `Interaction` type
3. **Complete History**: Both sensory input and actions are stored as separate, explicit entries
4. **Easy to Query**: Agent can recall "What did I do?" and "What did I sense?"
5. **Temporal Reasoning**: Agent knows exactly when each interaction occurred
6. **High Priority Actions**: Actions stored with importance 0.7 for better recall
7. **Extensible**: Easy to add new interaction types by inheriting from `Interaction`

## Configuration

### Change Time Sensor Interval

Edit `Program.cs` line 48:

```csharp
// Current: 30 seconds
agent.Sensors.Add(new TimeSensor(TimeSpan.FromSeconds(30)));

// Examples:
agent.Sensors.Add(new TimeSensor(TimeSpan.FromMinutes(1)));  // 1 minute
agent.Sensors.Add(new TimeSensor(TimeSpan.FromSeconds(15))); // 15 seconds
```

### Action Importance

To change the default importance for actions, edit `Agent.cs` line 155:

```csharp
ImportanceScore = 0.7f // Current: high priority
// Lower: 0.5f for medium priority
// Higher: 0.9f for very high priority
```

## Usage Example

### Testing Temporal Awareness and Action Memory

1. Start two agents:
```bash
# Terminal 1
dotnet run --Port 10000 --Name "AgentZero"

# Terminal 2
dotnet run --Port 10001 --Name "AgentOne"
```

2. AgentOne sends message to AgentZero

3. Wait 30+ seconds for TimeSensor to fire

4. AgentOne becomes aware of time elapsed and can reason:
   - "I sent a message 1 minute ago" (recalls ActionData)
   - "I haven't received a response" (no new SensoryData)
   - "I should follow up" (autonomous decision)

### Console Output

You'll now see proper typing of interactions:

```
Sensor MessageBoxSensor received data.
Data: Message from AgentTwo: "What's the status?"
[Memory] Stored sensory input in long-term memory (importance: 0.65)
Tool Call: send_message with arguments {"recipient":"AgentTwo","message":"Processing..."}
[Memory] Stored action 'send_message' in memory (importance: 0.70)
------------------------------------------
Sensor TimeSensor received data.
Data: Time update: Current UTC time is 2026-02-23 14:30:00. 30 seconds have elapsed...
[LTM] Recalled 10 relevant memories:
  - [2026-02-23 14:29:30] Action: send_message (recipient=AgentTwo, ...)
  - [2026-02-23 14:29:00] Message from AgentTwo: "What's the status?"
[Memory] Stored sensory input in long-term memory (importance: 0.40)
```

## Extending the System

### Add New Interaction Types

To add a new type of interaction:

```csharp
public class MyInteractionData : Interaction
{
    public DateTime Timestamp { get; }

    public MyInteractionData()
    {
        Timestamp = DateTime.UtcNow;
    }

    public override string ProcessingInstructions =>
        "Description for the Brain";

    public override string Recall =>
        $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] My interaction";
}
```

Then store it in memory:
```csharp
var interaction = new MyInteractionData();
interaction.Thought = thoughtAboutIt;
await longTermMemory.RememberAsync(interaction, importance);
```

## Performance Considerations

- Long-term memory grows unbounded (stores everything)
- Embedding generation for every interaction (OpenAI API calls)
- For high-throughput scenarios, consider:
  - Periodic memory consolidation/archiving
  - Batch embedding generation
  - Memory pruning strategies

## Next Steps / Future Enhancements

Possible improvements:
1. Add memory consolidation (combine similar interactions)
2. Add memory archiving (move old interactions to cold storage)
3. Add configurable retention policies
4. Add temporal queries ("What did I do 5 minutes ago?")
5. Add scheduled actions ("Do X in 10 minutes")
6. Add interaction statistics and analytics
7. Add interaction filtering/search by type
