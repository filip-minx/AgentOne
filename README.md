# Minx.AgentOne

**AgentOne** is a self‑contained reasoning agent written in **C# / .NET 8**. It senses messages, thinks with the help of an LLM, and acts by sending messages. Use it to assemble distributed swarms of AI agents that can talk to each other and to people. Its modular architecture lets you plug in custom sensors and actuators.

## Quick start

### Prerequisites

- **.NET 8 SDK** – build & run the project
- Any **OpenAI‑compatible REST endpoint** listening at `http://localhost:1234`
- A `systemmap.yaml` file describing your agent topology (see example below)

#### Example `systemmap.yaml`

```yaml
# systemmap.yaml example
---
systemMap:
- localhost:10000/AgentZero
- localhost:10001/AgentOne
- localhost:10002/AgentTwo
```

### Build & run

```bash
git clone https://github.com/filip-minx/AgentOne.git
cd AgentOne/Minx.AgentOne

dotnet restore

dotnet run --Port 10001 --Name "AgentOne"
```

The agent will start, register a message box named **AgentOne** on port `10001`, and begin processing messages.

### Command‑line options

| Option   | Default    | Description                                 |
| -------- | ---------- | ------------------------------------------- |
| `--Port` | `10001`    | TCP port used for this agent                |
| `--Name` | `AgentOne` | Logical name of the agent / its message box |
