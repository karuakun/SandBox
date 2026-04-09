# AgentSkillsSample — ドキュメント

Microsoft.AgentFramework + AWS Bedrock（Claude）によるスーパーバイザー＋サブエージェント構成のサンプル実装。

## ドキュメント一覧

| ファイル | 内容 |
|---|---|
| [architecture.md](./architecture.md) | システム全体のアーキテクチャ、コンポーネント設計、依存関係 |
| [agents.md](./agents.md) | エージェント設計、スキルシステム、オーケストレーション詳細 |
| [database.md](./database.md) | DBスキーマ、シードデータ、スキル定義 |
| [api.md](./api.md) | APIエンドポイント、SSEストリーミング、Redis再接続 |

## クイックスタート

### 前提条件

- .NET 10 SDK
- AWS アカウント（Bedrock + Claude モデルへのアクセス権）
- （任意）Redis（SSE 再接続機能を使う場合）

### 環境変数設定

```bash
export AWS_ACCESS_KEY_ID=<your-access-key>
export AWS_SECRET_ACCESS_KEY=<your-secret-key>
export AWS_DEFAULT_REGION=us-east-1
```

### 起動

```bash
cd AgentSkillsSample/src/AgentSkillsSample.WebApi
dotnet run
```

初回起動時に `skills.db`（SQLite）が自動作成され、エージェント定義・スキルのシードデータが投入されます。

### API テスト

```bash
curl -N -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "AIエージェントの最新トレンドについて教えてください"}'
```

レスポンスはSSE（Server-Sent Events）形式でストリーミングされます:

```
data: [supervisor] 🔍 ResearchAgent を呼び出し中...

data: [supervisor] 🔎 FactCheckAgent を呼び出し中...

data: [supervisor] 📝 ReportAgent を呼び出し中...

data: [supervisor] ✅ ValidationAgent で品質検証中...

data: [supervisor] 判定: Complete — 要件が充足されています

data: [supervisor] ✅ 要求を充足。最終レポートを出力します。

data: # AIエージェント最新トレンド
data: ...

data: [DONE]
```
