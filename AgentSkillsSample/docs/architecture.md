# アーキテクチャ概要

## システム構成

```
┌─────────────────────────────────────────────────────────────────┐
│                    AgentSkillsSample.WebApi                      │
│  ASP.NET Core Minimal API (.NET 10)                              │
│                                                                   │
│  POST /api/chat ──► SupervisorAgent ──► SSE Stream              │
│  POST /api/messages ──► SupervisorAgent (Bot Protocol)          │
└─────────────────────────────────────────────────────────────────┘
         │                        │
         ▼                        ▼
  ┌─────────────┐        ┌──────────────────┐
  │  SQLite DB  │        │   AWS Bedrock    │
  │  skills.db  │        │  Claude Models   │
  │             │        │  sonnet/opus/    │
  │  Agents     │        │  haiku           │
  │  Skills     │        └──────────────────┘
  │  Mappings   │
  └─────────────┘
         │
         ▼ (Optional)
  ┌─────────────┐
  │    Redis    │
  │  SSE Events │
  │  TTL: 5min  │
  └─────────────┘
```

## コンポーネント一覧

| コンポーネント | クラス | 役割 |
|---|---|---|
| エンドポイント | `ChatEndpoints` | SSE ストリーミング、再接続処理 |
| スーパーバイザー | `SupervisorAgent` | オーケストレーション、ループ制御 |
| リサーチ | `ResearchAgent` | 情報収集・要約 |
| ファクトチェック | `FactCheckAgent` | 事実確認・信頼性評価 |
| レポート | `ReportAgent` | レポート文書生成 |
| バリデーション | `ValidationAgent` | 品質検証・判定 |
| スキルプロバイダー | `DbSkillsProvider` | DB からスキルを動的ロード |
| SSE ストア | `ISseEventStore` | イベント永続化・再送信 |

## 依存関係マップ

```
Program.cs
  ├── AgentDbContext (SQLite / EF Core)
  │     ├── Agents テーブル        ← AgentDefinition エンティティ
  │     ├── Skills テーブル        ← SkillDefinition エンティティ
  │     └── AgentSkillMappings    ← AgentSkillMapping エンティティ
  │
  ├── IAgentRepository → AgentRepository
  ├── ISkillsRepository → SkillsRepository
  │
  ├── Func<string, DbSkillsProvider>  ← agentId 別にスコープ内で生成
  │     ├── ISkillsRepository
  │     ├── IAgentRepository
  │     └── IKeyedServiceProvider → IChatClient[modelKey]
  │
  ├── IChatClient["sonnet"] → AnthropicBedrockClient (Singleton)
  ├── IChatClient["opus"]   → AnthropicBedrockClient (Singleton)
  ├── IChatClient["haiku"]  → AnthropicBedrockClient (Singleton)
  ├── IChatClient           → 上記 "default" キーへの委譲 (Singleton)
  │
  ├── ResearchAgent    → DbSkillsProvider("research-agent")
  ├── FactCheckAgent   → DbSkillsProvider("fact-check-agent")
  ├── ReportAgent      → DbSkillsProvider("report-agent")
  ├── ValidationAgent  → DbSkillsProvider("validation-agent")
  ├── SupervisorAgent  → ResearchAgent, FactCheckAgent, ReportAgent, ValidationAgent
  │
  ├── ISseEventStore → InMemorySseEventStore  (Redis:Enabled = false)
  │                 → RedisSseEventStore      (Redis:Enabled = true)
  │
  └── ChatEndpoints → SupervisorAgent, ISseEventStore
```

## NuGet パッケージ

| パッケージ | バージョン | 用途 |
|---|---|---|
| `Microsoft.Agents.Core` | 1.4.83 | Activity Protocol・コア IF |
| `Microsoft.Agents.Builder` | 1.4.83 | `IAgent` インターフェース |
| `Microsoft.Agents.Hosting.AspNetCore` | 1.4.83 | DI・ASP.NET Core 統合 |
| `Anthropic` | 12.11.0 | Claude SDK 本体 |
| `Anthropic.Bedrock` | 0.1.2 | AWS Bedrock 経由 Claude 接続 |
| `Microsoft.Extensions.AI.Abstractions` | — | `IChatClient` 標準 IF |
| `Microsoft.EntityFrameworkCore.Sqlite` | — | SQLite 永続化 |
| `StackExchange.Redis` | 2.x | SSE 再接続用イベント永続化 |

## ファイル構成

```
AgentSkillsSample/
├── AgentSkillsSample.slnx
└── src/
    └── AgentSkillsSample.WebApi/
        ├── AgentSkillsSample.WebApi.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── Agents/
        │   ├── AgentBase.cs           # 共通基底クラス（スキルロードロジック）
        │   ├── AgentReport.cs         # サブエージェント → スーパーバイザー報告型
        │   ├── ValidationResult.cs    # ValidationAgent の判定型
        │   ├── SupervisorAgent.cs     # オーケストレーション
        │   ├── ResearchAgent.cs       # ①情報収集
        │   ├── FactCheckAgent.cs      # ②事実確認
        │   ├── ReportAgent.cs         # ③レポート生成
        │   └── ValidationAgent.cs     # ④品質検証
        ├── Skills/
        │   ├── AgentDefinition.cs     # EF Core エンティティ
        │   ├── SkillDefinition.cs     # EF Core エンティティ
        │   ├── AgentSkillMapping.cs   # EF Core エンティティ
        │   ├── IAgentRepository.cs
        │   ├── AgentRepository.cs
        │   ├── ISkillsRepository.cs
        │   ├── SkillsRepository.cs
        │   └── DbSkillsProvider.cs    # スキル動的ロードのコア実装
        ├── Data/
        │   ├── AgentDbContext.cs
        │   ├── AgentDbContextFactory.cs
        │   └── SeedData.cs
        ├── Endpoints/
        │   └── ChatEndpoints.cs
        └── Sse/
            ├── ISseEventStore.cs
            ├── InMemorySseEventStore.cs
            └── RedisSseEventStore.cs
```

## 設定（appsettings.json）

```json
{
  "Bedrock": {
    "Region": "us-east-1",
    "Models": {
      "sonnet":  "us.anthropic.claude-sonnet-4-5-20250929-v1:0",
      "opus":    "us.anthropic.claude-opus-4-6-20260101-v1:0",
      "haiku":   "us.anthropic.claude-haiku-4-5-20251001-v1:0",
      "default": "sonnet"
    }
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": "localhost:6379",
    "SseEventTtlSeconds": 300
  },
  "ConnectionStrings": {
    "AgentDb": "Data Source=skills.db"
  }
}
```

### 認証（環境変数）

AWS 認証情報はアプリケーション設定には含めず、環境変数で渡します:

```bash
AWS_ACCESS_KEY_ID=<key>
AWS_SECRET_ACCESS_KEY=<secret>
AWS_DEFAULT_REGION=us-east-1
```

`AnthropicBedrockCredentialsHelper.FromEnv()` が起動時に読み込みます。
