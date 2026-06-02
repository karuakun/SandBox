# MacroDroid Automation Manager — AI Agent ガイド

このドキュメントは AI エージェント（GitHub Copilot、Codex、その他）が
このリポジトリで作業する際の参照情報です。

---

## プロジェクト概要

**目的**: Android 自動化アプリ MacroDroid のマクロを、PC 上の YAML で設計・管理し、
Dropbox 経由でスマートフォンへデプロイするシステム。

**技術スタック**: TypeScript / Node.js / Electron / React / Dropbox API / Anthropic SDK

**monorepo**: npm workspaces

---

## ディレクトリ構造と役割

```
macrodroid-manager/
├── scenarios/          # ★ YAML ソース（ユーザーが編集）
├── scripts/            # ★ Termux シェルスクリプト（ユーザーが編集）
├── dist/               # ★ 変換成果物（npm run build:scenarios で生成・Git管理）
│   └── macrodroid/
├── converters/         # ユーザー定義変換器（PlatformConverter を実装）
├── exports/            # スマホからエクスポートされた .mdr → YAML
├── tools/
│   └── create-converter.ts  # Claude API による変換器コード生成
└── packages/
    ├── converter/       # コアライブラリ
    │   └── src/
    │       ├── types/
    │       │   ├── schema.ts      # YAML Scenario 型定義
    │       │   ├── mdr.ts         # MacroDroid .mdr 型定義
    │       │   └── converter.ts   # PlatformConverter インターフェース（T1-8）
    │       ├── converters/
    │       │   └── macrodroid/    # 組み込み MacroDroid 変換器（T1-9）
    │       ├── registry.ts        # ConverterRegistry（T1-8）
    │       ├── build.ts           # buildScenarios()（T1-10）
    │       ├── validator.ts       # zod バリデーター（実装済み）
    │       ├── yaml-to-mdr.ts     # 変換ロジック（実装済み）
    │       └── mdr-to-yaml.ts     # 逆変換ロジック（実装済み）
    ├── providers/
    │   └── src/
    │       ├── types/provider.ts  # DeployProvider インターフェース（実装済み）
    │       └── dropbox-provider.ts # Dropbox 実装（実装済み）
    └── app/                       # Electron アプリ（Phase 2 未実装）
```

---

## 主要インターフェース

### PlatformConverter（`packages/converter/src/types/converter.ts` — 未作成）

```typescript
interface PlatformConverter {
  readonly platformId: string;
  readonly displayName: string;
  readonly outputExtension: string;
  readonly version: string;
  convert(scenario: Scenario): Promise<ConvertResult>;
  importFrom?(input: Buffer, filename: string): Promise<ImportResult>;
}
```

### DeployProvider（`packages/providers/src/types/provider.ts` — 実装済み）

```typescript
interface DeployProvider {
  testConnection(): Promise<boolean>;
  deployScenario(scenario: Scenario, mdrContent: Buffer): Promise<DeployResult>;
  deployScript(scriptName: string, content: string): Promise<DeployResult>;
  listExports(): Promise<ExportEntry[]>;
  fetchExport(entry: ExportEntry): Promise<Buffer>;
  deleteExport(entry: ExportEntry): Promise<void>;
}
```

### Scenario（`packages/converter/src/types/schema.ts` — 実装済み）

```typescript
interface Scenario {
  apiVersion: 'macrodroid/v1';
  kind: 'Scenario';
  metadata: ScenarioMetadata;
  macros: MacroDefinition[];
}
```

---

## コーディング規約

- TypeScript strict mode（`exactOptionalPropertyTypes: true`）
- ESM (`"type": "module"`)
- ファイル内で `import ... from './foo.js'`（`.js` 拡張子必須）
- 日本語のエラーメッセージ（バリデーターのユーザー向けメッセージ）
- 未確定の実装箇所には `// ⚠️` コメントを付ける

---

## Git 管理方針

| パス | Git 管理 | 理由 |
|------|---------|------|
| `scenarios/` | ✅ 追跡 | ソース |
| `scripts/` | ✅ 追跡 | ソース |
| `dist/` | ✅ 追跡 | 変換成果物（意図的） |
| `exports/` | ✅ 追跡 | インポート履歴 |
| `converters/` | ✅ 追跡 | ユーザー変換器 |
| `packages/*/dist/` | ❌ 除外 | TypeScript ビルド成果物 |
| `settings.yaml` | ❌ 除外 | 認証情報 |

---

## 現在のフェーズと次のタスク

**完了済み**: T0-3, T0-4, T1-1〜T1-7  
**次のタスク**: T1-8〜T1-12（変換器抽象化・ビルドコマンド・AI 生成ツール）

詳細は `docs/02_tasks.md` を参照。

---

## 注意事項

1. **`settings.yaml` を生成・コミットしない**
2. **`dist/` を手動編集しない**（`build:scenarios` コマンドで生成）
3. `.mdr` フォーマットの一部フィールド名は実機未確認（⚠️ コメント参照）
4. 変換器を新規作成する場合は `PlatformConverter` インターフェースに必ず従う
5. `converters/` のユーザー定義変換器は `export default class` で単一クラスをエクスポート
