# データベース設計

## スキーマ概要

SQLite（EF Core）による 3 テーブル構成。起動時に自動マイグレーション + シードデータ投入。

```
Agents テーブル          Skills テーブル
┌──────────────┐         ┌──────────────┐
│ Id (PK)      │         │ Id (PK)      │
│ AgentId (UQ) │         │ Name (UQ)    │
│ Name         │         │ Description  │
│ Description  │         │ Content      │
│ SystemPrompt │         │ ModelKey     │
│ DefaultModelKey        │ IsActive     │
│ IsActive     │         │ CreatedAt    │
│ CreatedAt    │         │ UpdatedAt    │
│ UpdatedAt    │         └──────┬───────┘
└──────────────┘                │
                                │ FK
         AgentSkillMappings テーブル
         ┌─────────────────────────────┐
         │ AgentId (PK, FK→Agents)    │
         │ SkillId (PK, FK→Skills.Id) │
         │ SortOrder                   │
         └─────────────────────────────┘
```

## Agents テーブル

エージェントの定義・システムプロンプトを管理します。

| カラム | 型 | 説明 |
|---|---|---|
| `Id` | INTEGER PK | 自動採番 |
| `AgentId` | TEXT UNIQUE | エージェント識別子（コード内の文字列キー） |
| `Name` | TEXT | 表示名 |
| `Description` | TEXT | エージェントの役割説明 |
| `SystemPrompt` | TEXT | LLM に渡すシステムプロンプト全文 |
| `DefaultModelKey` | TEXT \| NULL | デフォルトモデルキー。NULL の場合 appsettings の "default" を使用 |
| `IsActive` | INTEGER | 有効フラグ（1=有効） |
| `CreatedAt` / `UpdatedAt` | TEXT | タイムスタンプ（UTC ISO8601） |

## Skills テーブル

スキルの定義・実行コンテンツを管理します。

| カラム | 型 | 説明 |
|---|---|---|
| `Id` | INTEGER PK | 自動採番 |
| `Name` | TEXT UNIQUE | スキル識別子（小文字・ハイフン区切り） |
| `Description` | TEXT | スキルの短い説明（広告フェーズで LLM へ提示、~100 token） |
| `Content` | TEXT | スキルの詳細定義（SKILL.md 形式、<5000 token） |
| `ModelKey` | TEXT \| NULL | このスキル実行時に使うモデルキー。NULL の場合エージェントの `DefaultModelKey` にフォールバック |
| `IsActive` | INTEGER | 有効フラグ |
| `CreatedAt` / `UpdatedAt` | TEXT | タイムスタンプ |

## AgentSkillMappings テーブル

エージェントとスキルの多対多関係を管理します。

| カラム | 型 | 説明 |
|---|---|---|
| `AgentId` | TEXT PK | エージェント識別子（Agents.AgentId を参照） |
| `SkillId` | INTEGER PK | スキル ID（Skills.Id への FK） |
| `SortOrder` | INTEGER | スキルの提示順序 |

## シードデータ

### エージェント定義

| AgentId | Name | DefaultModelKey | 役割 |
|---|---|---|---|
| `research-agent` | ResearchAgent | sonnet | 情報収集・要約 |
| `fact-check-agent` | FactCheckAgent | opus | 事実確認・信頼性評価 |
| `report-agent` | ReportAgent | sonnet | レポート文書生成 |
| `validation-agent` | ValidationAgent | opus | 品質検証・判定（Complete/Retry/Abort） |

### スキル定義

| スキル名 | ModelKey | エージェント | 概要 |
|---|---|---|---|
| `web-search` | haiku | research-agent | Web 情報検索の手順と出力形式 |
| `text-summarization` | sonnet | research-agent | 長文テキストの構造化要約 |
| `source-verification` | opus | fact-check-agent | 情報ソースの信頼性評価（0-100点） |
| `claim-analysis` | sonnet | fact-check-agent | 論理的整合性・根拠の妥当性分析 |
| `data-analysis` | opus | report-agent | 比較・トレンド・因果分析 |
| `document-generation` | sonnet | report-agent | レポート文書の構成と生成ガイド |
| `requirement-check` | opus | validation-agent | 要件充足率の計算と Complete/Retry/Abort 判定 |
| `quality-assessment` | sonnet | validation-agent | 正確性・完全性・明確性・有用性の4軸評価 |

### スキルコンテンツ形式（SKILL.md）

各スキルの `Content` は以下の Markdown 形式で記述されています:

```markdown
# [スキル名] Skill

## 目的
（スキルが何のためにあるかを説明）

## 手順 / 分析手法 / 評価軸
（具体的な実行手順や評価基準）

## 出力形式
（LLM に期待する出力の構造）
```

## スキルの動的管理

シードデータ投入後も、SQLite を直接操作してスキルを追加・変更できます。アプリの再起動なしに次回リクエストから有効になります（リクエストスコープでキャッシュ）。

```sql
-- 新スキルの追加
INSERT INTO Skills (Name, Description, Content, ModelKey, IsActive, CreatedAt, UpdatedAt)
VALUES (
  'my-custom-skill',
  '新しいスキルの短い説明',
  '# My Custom Skill

## 目的
...

## 手順
...

## 出力形式
...',
  'sonnet',
  1,
  datetime('now'),
  datetime('now')
);

-- エージェントへのスキルマッピング
INSERT INTO AgentSkillMappings (AgentId, SkillId, SortOrder)
SELECT 'research-agent', Id, 3 FROM Skills WHERE Name = 'my-custom-skill';
```
