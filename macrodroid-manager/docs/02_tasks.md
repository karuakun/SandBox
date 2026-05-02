# MacroDroid Automation Manager タスク分割

**バージョン**: 0.3.0  
**作成日**: 2026-05-02  
**更新日**: 2026-05-02  
**参照**: [仕様書](./01_spec.md)

---

## フェーズ概要

```
Phase 0: 調査・検証        ✅ T0-3, T0-4 完了。T0-1, T0-2 は実機必須
Phase 1: 基盤実装          🔄 T1-1〜T1-7 完了。T1-8〜T1-22 追加（含テスト）
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

---

## Phase 1 テスト: 単体テスト・結合テスト

> **テスト方針**
> - フレームワーク: **vitest**（ESM ネイティブ・jest 互換 API）
> - カバレッジ: `@vitest/coverage-v8`（目標: converter 80%+、providers 70%+）
> - モック: `vi.mock()` / `vi.spyOn()`
> - テストファイル配置: 各パッケージの `src/__tests__/` ディレクトリ
> - 命名規則: `*.test.ts`

---

### T1-13: テスト環境セットアップ ⬜

**依存**: なし（T1-13 は他テストタスクすべての前提）

**成果物**:
- `packages/converter/package.json` に vitest 追加
- `packages/converter/vitest.config.ts`
- `packages/providers/vitest.config.ts`
- ルート `package.json` の `test` / `test:coverage` スクリプト

**設定例**:
```typescript
// vitest.config.ts
export default defineConfig({
  test: {
    globals: true,
    environment: 'node',
    coverage: { provider: 'v8', reporter: ['text', 'lcov'] },
  },
});
```

**npm スクリプト追加**:
```json
"test": "vitest run",
"test:watch": "vitest",
"test:coverage": "vitest run --coverage"
```

---

### T1-14: validator 単体テスト ⬜

**依存**: T1-13、T1-3（実装済み）  
**成果物**: `packages/converter/src/__tests__/validator.test.ts`

| テストケース | 内容 |
|------------|------|
| 正常系: 最小構成 | `time` トリガー + `notification` アクションのみ |
| 正常系: 全 trigger type | 各 type で valid なシナリオを1ケースずつ |
| 正常系: 全 action type | `if_else` のネストを含む |
| 正常系: 全 condition type | |
| 異常系: 必須フィールド欠如 | `metadata.id` なし、`actions` 空配列など |
| 異常系: 不正フォーマット | `time: "7:00"`（HH:mm 形式違反）|
| 異常系: 不正 ID | `id: "Morning Routine"`（スペース含む） |
| 境界値: if_else の深いネスト | 3段ネストが通ること |

---

### T1-15: yaml-to-mdr 単体テスト ⬜

**依存**: T1-13、T1-4（実装済み）  
**成果物**: `packages/converter/src/__tests__/yaml-to-mdr.test.ts`

| テストケース | 内容 |
|------------|------|
| TimerTrigger 変換 | `time: "07:00"`, `days: [mon, fri]` → `m_hour`, `m_daysOfWeek` |
| WifiConnectionTrigger 変換 | `wifi_connected` / `wifi_disconnected` の `m_wifiState` 値 |
| NotificationAction 変換 | `title` / `text` → 推定フィールド名へのマッピング |
| SetWifiAction 変換 | `state: enable` → `m_state` 整数値 |
| termux_script 変換 | `⚠️` 箇所が現状の推定値で出力されること（スナップショット） |
| raw type 透過 | 入力の `rawData` がそのまま出力に含まれること |
| GUID 生成 | `m_GUID` が毎回異なる整数値であること |
| カテゴリ抽出 | 複数マクロのカテゴリが `categoryList` に重複なく含まれること |

**スナップショットテスト**: `⚠️` 箇所は T0-1 完了後に正しい値で上書き確認する

---

### T1-16: mdr-to-yaml 単体テスト ⬜

**依存**: T1-13、T1-5（実装済み）  
**成果物**: `packages/converter/src/__tests__/mdr-to-yaml.test.ts`

| テストケース | 内容 |
|------------|------|
| TimerTrigger → `time` | `m_daysOfWeek: [1,2,3,4,5]` → `days: [mon,...,fri]` |
| ScreenOnOffTrigger → screen_on/off | `m_screenOn` で分岐 |
| DayOfWeekConstraint → `day_of_week` | インデックス → 曜日文字列 |
| 未知クラス → `raw` | `m_classType: 'UnknownAction'` → `type: 'raw'` + warnings |
| warnings 蓄積 | 複数未知クラスが含まれる場合に全件 warnings に記録 |
| scenarioId がメタデータに反映 | `mdrToScenario(mdr, 'test-id')` → `metadata.id === 'test-id'` |

---

### T1-17: MacroDroidConverter 単体テスト ⬜

**依存**: T1-13、T1-9（未実装）  
**成果物**: `packages/converter/src/__tests__/converters/macrodroid.test.ts`

| テストケース | 内容 |
|------------|------|
| `convert()` 出力形式 | JSZip で開けること（ZIP フォーマット） |
| `convert()` 内部構造 | ZIP 内に `macros` ファイルが存在し valid JSON であること |
| `convert()` filename | `{id}_v{version}.mdr` の形式 |
| `importFrom()` ZIP 入力 | convert() 出力をそのまま渡して Scenario が返ること |
| `importFrom()` plain JSON 入力 | ZIP でない場合のフォールバック動作 |
| `importFrom()` 未知クラス | warnings に記録されること |

---

### T1-18: ConverterRegistry 単体テスト ⬜

**依存**: T1-13、T1-8（未実装）  
**成果物**: `packages/converter/src/__tests__/registry.test.ts`

| テストケース | 内容 |
|------------|------|
| `register` / `get` / `list` | 基本的な登録・取得・一覧 |
| 同一 platformId の上書き | 後から登録したものが優先 |
| `loadFromDirectory` 正常系 | 有効な変換器クラスをディレクトリからロード |
| `loadFromDirectory` 不正ファイル | エラーを errors に記録してスキップ |
| `loadFromDirectory` 存在しないディレクトリ | 例外ではなく空の結果を返す |

---

### T1-19: DropboxProvider 単体テスト ⬜

**依存**: T1-13、T1-7（実装済み）  
**成果物**: `packages/providers/src/__tests__/dropbox-provider.test.ts`

**モック方針**: `vi.mock('dropbox')` で Dropbox SDK をモック

| テストケース | 内容 |
|------------|------|
| `testConnection()` 成功 | `usersGetCurrentAccount` が成功 → `true` |
| `testConnection()` 失敗 | SDK が throw → `false` |
| `deployScenario()` 成功 | `filesUpload` が正しいパス・内容で呼ばれること |
| `deployScenario()` 失敗 | SDK エラー → `DeployResult.success === false` |
| `deployScript()` パス構築 | `basePath/deploy/scripts/{name}` になること |
| `listExports()` 一覧取得 | `.mdr` のみフィルタリングされること |
| `listExports()` API エラー | 例外ではなく空配列を返すこと |
| `fetchExport()` | `filesDownload` が呼ばれ Buffer が返ること |
| `deleteExport()` | `filesDeleteV2` が正しいパスで呼ばれること |

---

### T1-20: YAML → .mdr → YAML ラウンドトリップ結合テスト ⬜

**依存**: T1-9（MacroDroidConverter）  
**成果物**: `packages/converter/src/__tests__/integration/roundtrip.test.ts`

**方針**: 実際の `scenarios/*.yaml` を使ってコンバーターを通した後、
主要フィールドが元の値と一致することを確認する。

| テストケース | 内容 |
|------------|------|
| morning-routine ラウンドトリップ | `scenarios/morning-routine.yaml` 全体 |
| system-health-check ラウンドトリップ | termux_script を含むシナリオ |
| id の保持 | `metadata.id` が変換前後で一致 |
| マクロ数の保持 | `macros.length` が一致 |
| trigger type の保持 | 各マクロの trigger.type が一致 |
| action type の保持 | 各アクションの type が一致（raw になっていないこと） |

> **注意**: T0-1 完了前は `termux_script` の逆変換が `raw` になる可能性あり。  
> その場合はそのまま failing test として残し、T0-1 完了後に修正する。

---

### T1-21: buildScenarios 結合テスト ⬜

**依存**: T1-10（buildScenarios 実装）  
**成果物**: `packages/converter/src/__tests__/integration/build.test.ts`

**方針**: 一時ディレクトリを使い、実際の YAML ファイルからビルドを実行する。

| テストケース | 内容 |
|------------|------|
| 正常ビルド | `scenarios/*.yaml` が全て `dist/macrodroid/` に出力される |
| 出力ファイル名 | `{id}_v{version}.mdr` 形式であること |
| バリデーションエラー | 不正な YAML が含まれても他のビルドは続行し errors に記録 |
| 複数変換器 | MacroDroid + サンプル変換器の両方で出力が生成される |
| 空の scenariosDir | エラーではなく空の結果が返ること |

---

### T1-22: ConverterRegistry + ユーザー定義変換器 結合テスト ⬜

**依存**: T1-11（ユーザー定義変換器サンプル）  
**成果物**: `packages/converter/src/__tests__/integration/user-converter.test.ts`

| テストケース | 内容 |
|------------|------|
| `converters/example/` の動的ロード | RegistryRegistry に登録されること |
| サンプル変換器での `convert()` | Scenario を受け取り Buffer を返すこと |
| 組み込み + ユーザー定義の共存 | `registry.list()` に両方が含まれること |

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

### T3-4: 実機 .mdr ファイルを使った変換テスト ⬜

**依存**: T0-1（実機エクスポート完了後）  
**成果物**: `packages/converter/src/__tests__/integration/real-mdr.test.ts`

**方針**: T0-1 で取得した実際の .mdr ファイルをテストフィクスチャとして使い、
変換器の実際の精度を確認する。

| テストケース | 内容 |
|------------|------|
| 最小構成 .mdr の importFrom | エラーなしで Scenario が返ること |
| 全クラス .mdr の importFrom | 各マクロの trigger/action が `raw` でないこと |
| Termux スクリプト .mdr | `termux_script` type として変換されること（T0-1 確認後） |
| 変換後の再変換 | importFrom → convert → importFrom で同じ Scenario になること |

**フィクスチャ配置**: `packages/converter/src/__tests__/fixtures/real/`  
（.mdr ファイルは T0-1 完了後に追加）

---

## Phase 4: ビジュアルワークフローエディタ（将来）

> **スコープ外**（現フェーズでは実装しない）

- ノードベースの GUI でトリガー・条件・アクションを配置
- YAML との双方向同期
- React Flow または XY Flow を使用予定

---

## 実装優先度・依存関係

```
【凡例】✅=完了  ⬜=未着手  実=実機必須

T0-1（実） ──────────────────────────────────────────────┐
T0-2（実） ────────────────────────────────┐             │
                                           │             │
T1-1 ✅                                    │             │
T1-2 ✅                                    │             │
T1-3 ✅ ──────────────────── T1-14         │             │
T1-4 ✅ ──────────────────── T1-15         │             │
T1-5 ✅ ──────────────────── T1-16         │             │
T1-6 ✅                                    │             │
T1-7 ✅ ──────────────────── T1-19         │             │
                                           │             │
T1-13（テスト環境）←─────── T1-14〜T1-22 すべての前提  │
                                           │             │
T1-8 ──── T1-18                            │             │
  │                                        │             │
T1-9 (T1-4,T1-5,T1-8) ──── T1-17         │             │
  │            └───────── T1-20（結合）    │             │
T1-10 (T1-9,T1-8) ──────── T1-21（結合）  │             │
T1-11 (T1-8) ────────────── T1-22（結合）  │             │
T1-12 (T1-8)                              │             │
                                          │             │
                                          ▼             ▼
                                   T3-2  T3-1       T3-4（実機テスト）
                                                        ↑
                                                    T0-1 完了後
T1-8〜T1-22 完了
        │
       T2-1
        │
       T2-2
        │
   ┌────┼──────────────────────┐
   ▼    ▼    ▼    ▼    ▼       ▼
 T2-3 T2-4 T2-5 T2-6 T2-7   T2-8  T2-9
   │                           │     │
   └───────────────────────── T2-10 ─┘
                                 │
                                T3-3
```

---

## 進捗トラッカー

> チェックボックス形式の一覧は `STATUS.md` も参照。

| タスク | 種別 | ステータス | 担当 | 完了日 |
|--------|------|-----------|------|--------|
| T0-1 | 調査 | ⬜ 未着手（実機必須） | | |
| T0-2 | 調査 | ⬜ 未着手（実機必須） | | |
| T0-3 | 決定 | ✅ 完了 | | 2026-05-02 |
| T0-4 | 決定 | ✅ 完了 | | 2026-05-02 |
| T1-1 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-2 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-3 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-4 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-5 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-6 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-7 | 実装 | ✅ 完了 | | 2026-05-02 |
| T1-8 | 実装 | ⬜ 未着手 | | |
| T1-9 | 実装 | ⬜ 未着手 | | |
| T1-10 | 実装 | ⬜ 未着手 | | |
| T1-11 | 実装 | ⬜ 未着手 | | |
| T1-12 | 実装 | ⬜ 未着手 | | |
| T1-13 | テスト | ⬜ 未着手 | | |
| T1-14 | 単体テスト | ⬜ 未着手 | | |
| T1-15 | 単体テスト | ⬜ 未着手 | | |
| T1-16 | 単体テスト | ⬜ 未着手 | | |
| T1-17 | 単体テスト | ⬜ 未着手（T1-9 後） | | |
| T1-18 | 単体テスト | ⬜ 未着手（T1-8 後） | | |
| T1-19 | 単体テスト | ⬜ 未着手 | | |
| T1-20 | 結合テスト | ⬜ 未着手（T1-9 後） | | |
| T1-21 | 結合テスト | ⬜ 未着手（T1-10 後） | | |
| T1-22 | 結合テスト | ⬜ 未着手（T1-11 後） | | |
| T2-1 | 実装 | ⬜ 未着手 | | |
| T2-2 | 実装 | ⬜ 未着手 | | |
| T2-3 | 実装 | ⬜ 未着手 | | |
| T2-4 | 実装 | ⬜ 未着手 | | |
| T2-5 | 実装 | ⬜ 未着手 | | |
| T2-6 | 実装 | ⬜ 未着手 | | |
| T2-7 | 実装 | ⬜ 未着手 | | |
| T2-8 | 実装 | ⬜ 未着手 | | |
| T2-9 | 実装 | ⬜ 未着手 | | |
| T2-10 | 実装 | ⬜ 未着手 | | |
| T3-1 | ドキュメント | ⬜ 未着手 | | |
| T3-2 | 実装 | ⬜ 未着手 | | |
| T3-3 | E2E テスト | ⬜ 未着手 | | |
| T3-4 | 実機テスト | ⬜ 未着手（T0-1 後） | | |
