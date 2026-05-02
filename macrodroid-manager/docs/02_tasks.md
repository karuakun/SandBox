# MacroDroid Automation Manager タスク分割

**バージョン**: 0.1.0  
**作成日**: 2026-05-02  
**参照**: [仕様書](./01_spec.md)

---

## フェーズ概要

```
Phase 0: 調査・検証        ← ここから始める（リスク解消）
Phase 1: 基盤実装          ← 変換ロジック・プロバイダー
Phase 2: PC アプリ実装     ← React UI
Phase 3: スマホ連携整備    ← セットアップガイド・テスト
Phase 4: ビジュアルエディタ ← 将来タスク（スコープ外）
```

---

## Phase 0: 調査・検証

仕様書のオープン事項を実装前に解消する。

### T0-1: MacroDroid .mdr フォーマット解析

**目的**: YAML ↔ .mdr 変換の実装根拠を固める

**作業内容**:
1. MacroDroid で以下のパターンのマクロを各1つ作成・エクスポート
   - 時刻トリガー + 通知アクション（最小構成）
   - Wi-Fi トリガー + Wi-Fi アクション
   - Webhook トリガー + HTTP リクエストアクション
   - Termux スクリプト実行アクション
   - 条件（曜日・バッテリー）付きマクロ
2. .mdr を unzip して内部 JSON を確認
3. クラス名・フィールド名のマッピング表を作成
4. 変換テーブルを `docs/03_mdr_format.md` にまとめる

**完了基準**: 最低5パターンのマッピングが確認できる

---

### T0-2: MacroDroid プログラム的インポート手段の検証

**目的**: スマホ側の自動インポートが実現可能か確認する

**作業内容**:
1. MacroDroid が `.mdr` ファイルを受け取る Intent を調査
   - `android.intent.action.VIEW` + MIME type `application/mdr` など
2. MacroDroid の「ファイルから実行」アクションでインポートできるか確認
3. Termux から `am` コマンドで Intent を発火してインポートできるか確認

**実現可能な場合**: スマホ側受信マクロのテンプレートを `docs/04_phone_setup.md` に記載  
**実現不可能な場合**: 代替手段（手動インポート + 通知のみ）に仕様を更新

---

### T0-3: アプリ形態の決定（Electron vs Web）

**目的**: PC アプリの技術選択を確定させる

**判断基準**:

| 観点 | Electron | Web (ブラウザ) |
|------|----------|----------------|
| ローカル Git リポジトリ操作 | ネイティブ fs API | File System Access API（要ユーザー許可） |
| クラウド API 呼び出し | 制限なし | CORS 制約あり |
| インストール | 必要 | 不要 |
| 配布コスト | 高い | 低い |

**推奨**: ローカルファイル操作の頻度と複雑さから **Electron** を推奨  
**決定後**: 仕様書 Section 8 を更新

---

### T0-4: クラウドストレージ選定確定

**目的**: 最初に実装する DeployProvider を決める

**推奨**: **Dropbox**（API がシンプル、OAuth フローが軽量）

**確認事項**:
- Dropbox API v2 の Token 取得フローを確認
- スマホ側 Dropbox アプリの自動同期設定を確認（オフライン設定に注意）

---

## Phase 1: 基盤実装

### T1-1: リポジトリ・プロジェクト構造セットアップ

**作業内容**:
```
macrodroid-manager/
├── packages/
│   ├── converter/      # YAML ↔ .mdr 変換ライブラリ（T1-3, T1-4）
│   ├── providers/      # DeployProvider 実装（T1-5, T1-6）
│   └── app/            # React アプリ（Phase 2）
├── scenarios/
├── scripts/
├── exports/
├── docs/
├── package.json        # workspaces（monorepo）
└── tsconfig.base.json
```

**使用技術**: npm workspaces または pnpm workspaces

---

### T1-2: YAML スキーマの TypeScript 型定義

**成果物**: `packages/converter/src/types/schema.ts`

```typescript
// 主要な型の例
type Scenario = {
  apiVersion: 'macrodroid/v1';
  kind: 'Scenario';
  metadata: ScenarioMetadata;
  macros: MacroDefinition[];
};

type TriggerType = 'time' | 'interval' | 'wifi_connected' | ...;
type ActionType = 'notification' | 'wifi' | 'termux_script' | ...;
```

**含める内容**: 仕様書 Section 4 の全トリガー・アクション・条件

---

### T1-3: YAML バリデーター実装

**成果物**: `packages/converter/src/validator.ts`

**仕様**:
- `zod` でスキーマバリデーション
- エラーメッセージは日本語化（エディタのインラインエラー表示用）
- 未知の `type` は警告（エラーにしない）

---

### T1-4: YAML → .mdr コンバーター実装

**成果物**: `packages/converter/src/yaml-to-mdr.ts`

**実装順序**:
1. `.mdr` ZIP 生成の骨格（JSZip）
2. Scenario → MacroDroid JSON 変換の骨格
3. 各トリガー変換（T0-1 のマッピング表を参照）
4. 各アクション変換（`termux_script` を含む）
5. 各条件変換
6. GUID 自動生成

**テスト**: T0-1 で取得した .mdr ファイルと出力を比較

---

### T1-5: .mdr → YAML コンバーター実装

**成果物**: `packages/converter/src/mdr-to-yaml.ts`

**仕様**:
- T1-4 の逆変換
- 未知クラスは `type: raw` + `rawData` フィールドで保持
- 変換できた割合をメタデータに付記

---

### T1-6: DeployProvider インターフェース定義

**成果物**: `packages/providers/src/types/provider.ts`

```typescript
interface DeployProvider {
  deployScenario(scenario: Scenario, mdrContent: Buffer): Promise<void>;
  deployScript(scriptName: string, content: string): Promise<void>;
  listExports(): Promise<ExportEntry[]>;
  fetchExport(entry: ExportEntry): Promise<Buffer>;
  testConnection(): Promise<boolean>;
}
```

---

### T1-7: DropboxProvider 実装

**成果物**: `packages/providers/src/dropbox-provider.ts`

**使用**: Dropbox SDK for JavaScript (`dropbox` npm パッケージ)

**実装内容**:
- OAuth2 認証フロー（PKCE）
- `deployScenario`: `deploy/scenarios/` にアップロード
- `deployScript`: `deploy/scripts/` にアップロード
- `listExports`: `export/` フォルダのファイル一覧取得
- `fetchExport`: ファイルダウンロード
- `testConnection`: アカウント情報取得で疎通確認

**設定**: `settings.yaml` から Access Token を読み込む

---

## Phase 2: PC アプリ実装

### T2-1: Vite + React + TypeScript + Electron 初期化

**成果物**: `packages/app/` の雛形

**セットアップ**:
- `electron-vite` を使用
- shadcn/ui セットアップ
- Monaco Editor インストール
- ルーティング: React Router（ハッシュルーター）

---

### T2-2: レイアウト・ナビゲーション

**成果物**: サイドバー付きのシェルレイアウト

**要素**:
- サイドバー: Scenarios / Scripts / Exports / Settings
- 各セクションでのナビゲーション
- 接続ステータスインジケーター（プロバイダー接続状態）

---

### T2-3: シナリオ一覧画面

**成果物**: `/scenarios` 画面

**機能**:
- `scenarios/*.yaml` の一覧表示
- 各シナリオのカード: 名前・マクロ数・最終更新日・有効/無効切替
- 「新規作成」ボタン
- 「デプロイ」ボタン（個別・一括）
- 「削除」ボタン（確認ダイアログ付き）

---

### T2-4: シナリオ編集画面（YAML エディタ）

**成果物**: `/scenarios/:id` 画面

**機能**:
- Monaco Editor で YAML を直接編集
- リアルタイムバリデーション（zod）
- エラーをエディタ下部に表示
- 「保存」「デプロイ」「キャンセル」ボタン
- マクロ一覧のプレビュー表示（YAML の右ペイン）

---

### T2-5: シナリオ新規作成

**成果物**: `/scenarios/new` 画面

**機能**:
- テンプレート選択
  - 空のシナリオ
  - 時刻トリガー（基本）
  - Termux スクリプト統合（高度）
- シナリオ ID・名前の入力
- YAML エディタに遷移

---

### T2-6: スクリプト管理画面

**成果物**: `/scripts` 画面

**機能**:
- `scripts/*.sh` の一覧表示
- Monaco Editor（Shell 構文ハイライト）で編集
- 新規スクリプト作成
- スクリプト単体デプロイ

---

### T2-7: エクスポート管理画面

**成果物**: `/exports` 画面

**機能**:
- クラウドストレージの `export/` フォルダを監視・一覧表示
- 選択した .mdr を YAML に変換してプレビュー
- 「保存」で `exports/` または `scenarios/` に保存
- 変換できなかったフィールドの警告表示

---

### T2-8: 設定画面

**成果物**: `/settings` 画面

**機能**:
- プロバイダー選択（Dropbox / Google Drive / ... ）
- 認証情報入力フォーム（プロバイダー別に動的レンダリング）
- 「接続テスト」ボタン + 結果表示
- クラウドストレージ上のベースパス設定
- settings.yaml への保存

---

### T2-9: デプロイ機能の結合

**成果物**: シナリオ一覧・編集画面からのデプロイ動作

**フロー**:
1. YAML バリデーション
2. YAML → .mdr 変換
3. 関連スクリプトの収集
4. DeployProvider 経由でアップロード
5. 成功/失敗のトースト通知

---

## Phase 3: スマホ連携整備

### T3-1: スマホ側セットアップガイド作成

**成果物**: `docs/04_phone_setup.md`

**内容**:
- MacroDroid 受信マクロの設定手順（スクリーンショット付き）
- Termux セットアップ手順
- クラウドストレージ同期フォルダの設定
- T0-2 の検証結果に基づくインポート手順

---

### T3-2: 受信マクロのテンプレート作成

**成果物**: `scenarios/_system/receiver.yaml`（スマホ初期設定用）

**内容**:
- ファイル変更トリガー（deploy/scenarios/ 監視）
- スクリプトコピーアクション
- インポートアクション（T0-2 の結果次第）

このシナリオ自体を最初に手動インポートする手順を T3-1 に記載。

---

### T3-3: E2E テスト

**内容**:
1. PC でサンプルシナリオを作成
2. デプロイ実行
3. スマホ側で MacroDroid にマクロが追加されることを確認
4. Termux スクリプトが配置されることを確認
5. マクロを手動で実行して動作確認

---

## Phase 4: ビジュアルワークフローエディタ（将来）

> **スコープ外**（現フェーズでは実装しない）

- ノードベースの GUI でトリガー・条件・アクションを配置
- YAML との双方向同期
- React Flow または XY Flow を使用予定

---

## 実装優先度・依存関係

```
T0-1 ──────────────────────────────────┐
T0-2 ─────────────────────────┐        │
T0-3 ──┐                     │        │
T0-4 ──┘                     │        │
        │                    │        │
       T1-1                  │        │
        │                    │        │
       T1-2                  │        │
        │                    │        │
       T1-3                  │        │
        │                    ▼        ▼
       T1-6              T3-2       T1-4 ── T1-5
        │                            │
       T1-7                          │
        │                            │
       T2-1                          │
        │                            │
       T2-2                          │
        │                            │
    ┌───┼────────────────────┐       │
    ▼   ▼   ▼   ▼   ▼        ▼       │
   T2-3 T2-4 T2-5 T2-6 T2-7 T2-8    │
                              │      │
                             T2-9 ───┘
                              │
                             T3-3
```

---

## 進捗トラッカー

| タスク | ステータス | 担当 | 完了日 |
|--------|-----------|------|--------|
| T0-1 | 未着手 | | |
| T0-2 | 未着手 | | |
| T0-3 | 未着手 | | |
| T0-4 | 未着手 | | |
| T1-1 | 未着手 | | |
| T1-2 | 未着手 | | |
| T1-3 | 未着手 | | |
| T1-4 | 未着手 | | |
| T1-5 | 未着手 | | |
| T1-6 | 未着手 | | |
| T1-7 | 未着手 | | |
| T2-1 | 未着手 | | |
| T2-2 | 未着手 | | |
| T2-3 | 未着手 | | |
| T2-4 | 未着手 | | |
| T2-5 | 未着手 | | |
| T2-6 | 未着手 | | |
| T2-7 | 未着手 | | |
| T2-8 | 未着手 | | |
| T2-9 | 未着手 | | |
| T3-1 | 未着手 | | |
| T3-2 | 未着手 | | |
| T3-3 | 未着手 | | |
