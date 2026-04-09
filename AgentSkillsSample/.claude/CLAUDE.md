# AgentSkillsSample

Supervisor + Sub-agent sample using Microsoft.AgentFramework + AWS Bedrock (Claude).

## Build & Run

```bash
cd src/AgentSkillsSample.WebApi
dotnet build
dotnet run
```

Required env vars:
```
AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY
AWS_DEFAULT_REGION=us-east-1
```

## Project Layout

```
src/AgentSkillsSample.WebApi/
  Agents/       SupervisorAgent, ResearchAgent, FactCheckAgent, ReportAgent, ValidationAgent
  Skills/       DbSkillsProvider, repositories, EF Core entities
  Data/         AgentDbContext, SeedData
  Endpoints/    ChatEndpoints (SSE)
  Sse/          ISseEventStore, InMemorySseEventStore, RedisSseEventStore
docs/           Architecture documentation
```

## Key Conventions

- **DB-driven skills**: Skills and agent system prompts live in `skills.db` (SQLite). Add/modify skills via SQL without restarting.
- **Model keys**: `"sonnet"` / `"opus"` / `"haiku"` map to Bedrock model IDs in `appsettings.json`. Use `SkillDefinition.ModelKey` to assign per-skill.
- **load_skill pattern**: LLM responds with `load_skill("skill-name")` to request full skill content. `AgentBase` detects this via regex and re-calls the LLM (max 2 rounds).
- **ValidationAgent**: Must return pure JSON `{ verdict, reason, retryTarget?, retryInstruction? }`. Supervisor retries up to 3 times on `Retry` verdict.
- **SSE store**: Default is in-memory. Set `Redis:Enabled=true` in appsettings for multi-instance support.

## EF Core Migrations

```bash
# PATH may need: export PATH="$PATH:/root/.dotnet/tools"
cd src/AgentSkillsSample.WebApi
dotnet ef migrations add <MigrationName>
```

Migrations run automatically on startup via `db.Database.MigrateAsync()`.
