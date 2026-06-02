# MacroDroid Automation Manager — Claude Code ガイド

このドキュメントは Claude Code がこのリポジトリで作業する際の参照情報です。

---

## プロジェクト概要

MacroDroid（Android 自動化アプリ）のマクロ定義を PC の YAML で管理し、
Dropbox 経由でスマートフォンへデプロイするシステム。
Termux スクリプト統合による高度なワークフローにも対応。

---

## リポジトリ構造

```
macrodroid-manager/
├── docs/               # 仕様書・タスク・フォーマット解析
├── scenarios/          # YAML ソース（編集対象）
├── scripts/            # Termux スクリプト（編集対象）
├── dist/               # 変換成果物（Git 管理、自動生成）
├── converters/         # ユーザー定義変換器（動的ロード）
├── exports/            # スマホからのエクスポート
├── tools/              # CLI ツール
└── packages/
    ├── converter/      # 変換ライブラリ（コア）
    ├── providers/      # DeployProvider 実装
    └── app/            # Electron + React アプリ
```

---

## 重要なコマンド

```bash
# 型チェック
npm run typecheck --workspaces --if-present

# シナリオをビルド（YAML → dist/）
npm run build:scenarios

# Electron アプリ開発起動
npm run dev --workspace=packages/app
```

---

## アーキテクチャ上の制約・決定事項

### 変換器抽象化
- すべてのプラットフォーム変換は `PlatformConverter` インターフェースを実装する
- `packages/converter/src/types/converter.ts` の定義に必ず従う
- 組み込み変換器は `packages/converter/src/converters/` 以下に配置
- ユーザー定義変換器は `converters/` ルートに配置（動的ロード）

### dist/ の扱い
- `dist/` はシナリオの変換成果物。**Git 管理対象**
- `packages/*/dist/` は TypeScript ビルド成果物。**Git 管理外**（.gitignore 対象）
- `dist/` は直接編集しない（`npm run build:scenarios` で生成する）

### YAML スキーマ
- `apiVersion: macrodroid/v1` / `kind: Scenario` が必須
- `scenarios/` 内の YAML は `packages/converter/src/validator.ts` でバリデーション
- `type: raw` は未知クラスを保持するための脱出ハッチ（積極的に使わない）

### 認証情報
- `settings.yaml` は **絶対にコミットしない**（.gitignore 対象）
- `settings.yaml.example` をテンプレートとして使う

### .mdr フォーマット（MacroDroid）
- ZIP アーカイブ（内部に `macros` という JSON ファイル）の可能性が高い
- 実機検証未完了の箇所は `⚠️` コメントで明示済み（`T0-1` で要確認）
- `docs/03_mdr_format.md` にクラス名マッピング表がある

---

## 実装済みコンポーネント（Phase 0-1 完了分）

| ファイル | 内容 |
|--------|------|
| `packages/converter/src/types/schema.ts` | YAML スキーマ型定義 |
| `packages/converter/src/types/mdr.ts` | .mdr 内部型定義 |
| `packages/converter/src/validator.ts` | zod バリデーター |
| `packages/converter/src/yaml-to-mdr.ts` | YAML → MdrFile 変換ロジック |
| `packages/converter/src/mdr-to-yaml.ts` | MdrFile → YAML 変換ロジック |
| `packages/providers/src/types/provider.ts` | DeployProvider インターフェース |
| `packages/providers/src/dropbox-provider.ts` | DropboxProvider 実装 |

---

## 未実装・要注意箇所

- `T1-8〜T1-12`（`PlatformConverter` 抽象化・ビルドコマンド・AI生成ツール）は未実装
- Termux スクリプト実行の MacroDroid クラス名が未確定（実機確認待ち）
- `packages/app/` は `package.json` のみ存在、実装は Phase 2

---

## ドキュメント

| ファイル | 内容 |
|--------|------|
| `docs/01_spec.md` | 仕様書（v0.3.0） |
| `docs/02_tasks.md` | タスク分割・進捗トラッカー |
| `docs/03_mdr_format.md` | MacroDroid .mdr フォーマット解析 |
| `docs/04_converter_guide.md` | ユーザー変換器作成ガイド（未作成） |
| `docs/05_phone_setup.md` | スマホセットアップ手順（T0-2 完了後） |
