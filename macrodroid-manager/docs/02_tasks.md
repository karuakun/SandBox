# MacroDroid Automation Manager タスク分割

**バージョン**: 0.2.0  
**作成日**: 2026-05-02  
**更新日**: 2026-05-02  
**参照**: [仕様書](./01_spec.md)

---

## フェーズ概要

```
Phase 0: 調査・検証        ✅ T0-3, T0-4 完了。T0-1, T0-2 は実機必須
Phase 1: 基盤実装          🔄 T1-1〜T1-7 完了。T1-8〜T1-12 追加
Phase 2: PC アプリ実装     ⬜ 未着手
Phase 3: スマホ連携整備    ⬜ 未着手
Phase 4: ビジュアルエディタ ⬜ 将来タスク（スコープ外）
```

---

## Phase 0: 調査・検証

### T0-1: MacroDroid .mdr フォーマット解析 ⬜ 実機必須

**目的**: YAML ↔ .mdr 変換の実装根拠を固める（オンライン調査済み、実機確認残り）

**作業内容**:
1. MacroDroid で以下のパターンのマクロを各1つ作成・エクスポート
   - 時刻トリガー + 通知アクション（最小構成）→ **ファイル形式（ZIP か JSON か）確認**
   - Wi-Fi トリガー + Wi-Fi アクション
   - Webhook トリガー + HTTP リクエストアクション
   - **Termux スクリプト実行アクション** ← 最重要（クラス名未確定）
   - 条件（曜日・バッテリー）付きマクロ
2. `docs/03_mdr_format.md` のチェックリストを消し込む
3. アクションの実際の JSON フィールド名（`m_` プレフィックスの有無）を確認

**完了基準**: `docs/03_mdr_format.md` のチェックリスト全項目が ✅

---

### T0-2: MacroDroid プログラム的インポート手段の検証 ⬜ 実機必須

**目的**: スマホ側の自動インポートが実現可能か確認する

**作業内容**:
1. `android.intent.action.VIEW` + `.mdr` ファイルパスで MacroDroid が起動するか確認
2. Termux の `am` コマンドで Intent 発火を試みる
3. 可否によって `docs/05_phone_setup.md` の手順を確定する

---

### T0-3: Electron 採用決定 ✅ 完了

**決定**: **Electron**（electron-vite + React）

---

### T0-4: クラウドストレージ選定 ✅ 完了

**決定**: **Dropbox**（Dropbox SDK for JavaScript, API v2）

---

## Phase 1: 基盤実装

### T1-1: monorepo プロジェクト構造セットアップ ✅ 完了

**成果物**: 
- `package.json`（npm workspaces）
- `tsconfig.base.json`
- `packages/converter/`, `packages/providers/`, `packages/app/` 雛形

---

### T1-2: YAML スキーマの TypeScript 型定義 ✅ 完了

**成果物**: `packages/converter/src/types/schema.ts`  
全トリガー・アクション・条件の型定義、型ガード関数を含む。

---

### T1-3: YAML バリデーター実装 ✅ 完了

**成果物**: `packages/converter/src/validator.ts`  
zod による全スキーマバリデーション。日本語エラーメッセージ付き。

---

### T1-4: YAML → .mdr 変換ロジック実装 ✅ 完了

**成果物**: `packages/converter/src/yaml-to-mdr.ts`  
全既知クラスの変換ロジック。⚠️ 箇所は T0-1 で確認後に修正。

---

### T1-5: .mdr → YAML 変換ロジック実装 ✅ 完了

**成果物**: `packages/converter/src/mdr-to-yaml.ts`  
未知クラスは `type: raw` で保持。

---

### T1-6: DeployProvider インターフェース定義 ✅ 完了

**成果物**: `packages/providers/src/types/provider.ts`

---

### T1-7: DropboxProvider 実装 ✅ 完了

**成果物**: `packages/providers/src/dropbox-provider.ts`

---

### T1-8: PlatformConverter インターフェース + ConverterRegistry 実装 ⬜

**目的**: 変換器の抽象化。Tasker 等の将来対応・ユーザー定義変換器を可能にする。

**成果物**: 
- `packages/converter/src/types/converter.ts`
- `packages/converter/src/registry.ts`

**仕様**:
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

ConverterRegistry は `converters/` ディレクトリから動的ロードに対応。

---

### T1-9: MacroDroidConverter クラスへリファクタリング ⬜

**目的**: 既存の yaml-to-mdr.ts / mdr-to-yaml.ts を PlatformConverter として包む。

**成果物**: `packages/converter/src/converters/macrodroid/index.ts`

**仕様**:
- `class MacroDroidConverter implements PlatformConverter`
- `convert()`: YAML → ZIP（内部は JSON）として .mdr を生成（JSZip 使用）
- `importFrom()`: .mdr（ZIP または plain JSON）を YAML に変換

---

### T1-10: buildScenarios コマンド + dist/ Git 管理セットアップ ⬜

**目的**: `scenarios/*.yaml` → `dist/{platform}/*.ext` の一括変換コマンドを作る。

**成果物**:
- `packages/converter/src/build.ts`（`buildScenarios()` 関数）
- `tools/build-scenarios.ts`（CLI エントリポイント）
- `dist/macrodroid/.gitkeep`
- `.gitignore` 修正（`packages/*/dist/` のみ除外、ルート `dist/` は追跡）

**npm スクリプト**:
```json
"build:scenarios": "tsx tools/build-scenarios.ts"
```

---

### T1-11: ユーザー定義変換器サンプル + 動的ロード ⬜

**目的**: ユーザーが自分で変換器を作れることを実証する。

**成果物**:
- `converters/example/index.ts`（JSON 出力のサンプル変換器）
- ConverterRegistry の `loadFromDirectory()` で動的ロードできることを確認

**サンプル変換器の概要**:
```typescript
// converters/example/index.ts
export default class ExampleConverter implements PlatformConverter {
  readonly platformId = 'example';
  readonly displayName = 'Example JSON Export';
  readonly outputExtension = '.json';
  readonly version = '1.0.0';
  async convert(scenario: Scenario): Promise<ConvertResult> { ... }
}
```

---

### T1-12: Claude API 変換器生成ツール実装 ⬜

**目的**: 自然言語の説明からプラットフォーム変換器の TypeScript コードを AI 生成する。

**成果物**: `tools/create-converter.ts`

**使用技術**: `@anthropic-ai/sdk`（`claude-opus-4-7` を使用）

**仕様**:
```bash
# 使用方法
tsx tools/create-converter.ts <platformId> "<説明>"

# 例: Tasker 向け変換器を生成
tsx tools/create-converter.ts tasker \
  "Taskerの.xml形式への変換器。トリガーはProfile、アクションはTask、..."
```

**プロンプトに含める内容**:
1. `PlatformConverter` インターフェース定義（`types/converter.ts` の内容）
2. `Scenario` 型定義（`types/schema.ts` の内容）
3. `MacroDroidConverter` の実装例（完全なコード）
4. ユーザーが指定したプラットフォームの説明
5. 出力: TypeScript コードのみ（`\`\`\`typescript ... \`\`\`` 形式）

**出力先**: `packages/converter/src/converters/{platformId}/index.ts`

**プロンプトキャッシュ**: インターフェース定義と実装例はキャッシュ対象とする（`cache_control`）

---

## Phase 2: PC アプリ実装

### T2-1: Vite + React + TypeScript + Electron 初期化 ⬜

**成果物**: `packages/app/` の完全な骨格

**セットアップ**:
- `electron-vite` を使用（main / preload / renderer の3層構成）
- shadcn/ui セットアップ
- Monaco Editor インストール
- ルーティング: React Router（ハッシュルーター）

---

### T2-2: レイアウト・ナビゲーション ⬜

**成果物**: サイドバー付きシェルレイアウト

**サイドバー項目**: Scenarios / Scripts / Converters / Exports / Settings  
**ステータスバー**: プロバイダー接続状態・最終ビルド日時

---

### T2-3: シナリオ一覧画面 ⬜

**成果物**: `/scenarios` 画面

**機能**:
- `scenarios/*.yaml` の一覧表示（カード形式）
- 各カード: 名前・マクロ数・バージョン・最終更新日
- 「ビルド」ボタン: YAML → `dist/` に変換（選択した変換器で）
- 「デプロイ」ボタン: `dist/` をクラウドストレージに送信
- 「新規作成」「削除」ボタン

---

### T2-4: シナリオ編集画面（YAML エディタ） ⬜

**成果物**: `/scenarios/:id` 画面

**機能**:
- Monaco Editor で YAML 直接編集
- リアルタイム zod バリデーション
- 「保存」「ビルド」「デプロイ」ボタン
- 右ペイン: マクロ一覧プレビュー・変換結果のプラットフォーム別サマリー

---

### T2-5: シナリオ新規作成 ⬜

**成果物**: `/scenarios/new` 画面

テンプレート選択（空 / 時刻トリガー基本 / Termux 統合高度）

---

### T2-6: スクリプト管理画面 ⬜

**成果物**: `/scripts` 画面

Monaco Editor（Shell 構文ハイライト）でスクリプト管理

---

### T2-7: エクスポート管理画面 ⬜

**成果物**: `/exports` 画面

クラウドストレージの `export/` を監視 → .mdr → YAML 変換・確認・保存

---

### T2-8: コンバーター管理画面 ⬜

**成果物**: `/converters` 画面

**機能**:
- 登録済み変換器一覧（組み込み・ユーザー定義・状態）
- 「新規変換器を AI 生成」ボタン → AI 生成フォーム（プラットフォーム名・説明を入力）
- 生成進捗のストリーミング表示
- ユーザー定義変換器のリロード

---

### T2-9: 設定画面 ⬜

**成果物**: `/settings` 画面

- DeployProvider 選択・認証情報入力・接続テスト
- シナリオリポジトリのルートパス設定
- settings.yaml への保存

---

### T2-10: ビルド・デプロイ機能の結合 ⬜

**成果物**: T2-3/T2-4 からのビルド・デプロイ動作

**ビルドフロー**:
1. YAML バリデーション
2. 選択された変換器で変換
3. `dist/{platform}/` に書き出し
4. 成功/失敗通知

**デプロイフロー**:
1. `dist/` の対象ファイルを取得
2. DeployProvider 経由でアップロード
3. 関連スクリプトもアップロード

---

## Phase 3: スマホ連携整備

### T3-1: スマホ側セットアップガイド作成 ⬜

**成果物**: `docs/05_phone_setup.md`（T0-2 完了後に確定）

---

### T3-2: 受信マクロのテンプレート作成 ⬜

**成果物**: `scenarios/_system/receiver.yaml`  
ファイル変更トリガー + インポートアクション + スクリプトコピー

---

### T3-3: E2E テスト ⬜

PC でビルド・デプロイ → スマホで MacroDroid にマクロが追加されることを確認

---

## Phase 4: ビジュアルワークフローエディタ（将来）

> **スコープ外**（現フェーズでは実装しない）

- ノードベースの GUI でトリガー・条件・アクションを配置
- YAML との双方向同期
- React Flow または XY Flow を使用予定

---

## 実装優先度・依存関係

```
T0-1 ─────────────────────────────────────────┐
T0-2 ──────────────────────────────┐          │
                                   │          │
T1-1 ✅                            │          │
T1-2 ✅                            │          │
T1-3 ✅                            │          │
T1-4 ✅ ─────────────────────┐     │          │
T1-5 ✅                      │     │          │
T1-6 ✅                      │     │          │
T1-7 ✅                      │     │          │
                             │     │          │
T1-8 ──────────────────────┐ │     │          │
                           │ │     │          │
T1-9 (T1-4, T1-5, T1-8)──┐│ │     │          │
                          ││ │     │          │
T1-10 (T1-9, T1-8) ─────┐│ │ │     │          │
T1-11 (T1-8) ───────────┘│ │ │     │          │
T1-12 (T1-8) ────────────┘ │ │     │          │
                           │ │     │          │
                           │ ▼     ▼          ▼
                           │ T3-2  T3-1    T1-9（修正）
                           │
                           ▼
                          T2-1
                           │
                          T2-2
                           │
          ┌────────────────┼──────────────────┐
          ▼                ▼                  ▼
     T2-3〜T2-7          T2-8               T2-9
          │                │                  │
          └────────────────┴────── T2-10 ─────┘
                                       │
                                      T3-3
```

---

## 進捗トラッカー

| タスク | ステータス | 担当 | 完了日 |
|--------|-----------|------|--------|
| T0-1 | ⬜ 未着手（実機必須） | | |
| T0-2 | ⬜ 未着手（実機必須） | | |
| T0-3 | ✅ 完了 | | 2026-05-02 |
| T0-4 | ✅ 完了 | | 2026-05-02 |
| T1-1 | ✅ 完了 | | 2026-05-02 |
| T1-2 | ✅ 完了 | | 2026-05-02 |
| T1-3 | ✅ 完了 | | 2026-05-02 |
| T1-4 | ✅ 完了 | | 2026-05-02 |
| T1-5 | ✅ 完了 | | 2026-05-02 |
| T1-6 | ✅ 完了 | | 2026-05-02 |
| T1-7 | ✅ 完了 | | 2026-05-02 |
| T1-8 | ⬜ 未着手 | | |
| T1-9 | ⬜ 未着手 | | |
| T1-10 | ⬜ 未着手 | | |
| T1-11 | ⬜ 未着手 | | |
| T1-12 | ⬜ 未着手 | | |
| T2-1 | ⬜ 未着手 | | |
| T2-2 | ⬜ 未着手 | | |
| T2-3 | ⬜ 未着手 | | |
| T2-4 | ⬜ 未着手 | | |
| T2-5 | ⬜ 未着手 | | |
| T2-6 | ⬜ 未着手 | | |
| T2-7 | ⬜ 未着手 | | |
| T2-8 | ⬜ 未着手 | | |
| T2-9 | ⬜ 未着手 | | |
| T2-10 | ⬜ 未着手 | | |
| T3-1 | ⬜ 未着手 | | |
| T3-2 | ⬜ 未着手 | | |
| T3-3 | ⬜ 未着手 | | |
