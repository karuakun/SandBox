# MacroDroid Automation Manager 仕様書

**バージョン**: 0.1.0  
**作成日**: 2026-05-02  
**ステータス**: 草稿

---

## 1. プロジェクト概要

### 1.1 目的

Android の自動化アプリ MacroDroid の自動化タスク（マクロ）を、PC 側の TypeScript React アプリで設計・管理し、スマートフォンへデプロイする仕組みを構築する。

単純な MacroDroid アクションに加え、Termux スクリプトを組み合わせた高度なワークフローも同一の管理基盤で扱えるようにする。

### 1.2 スコープ

| 機能 | 説明 |
|------|------|
| シナリオ定義 | YAML で MacroDroid マクロ群を定義 |
| シナリオ管理 | 一覧表示・追加・編集・削除（PC 管理画面） |
| スクリプト管理 | Termux スクリプトを Git で一元管理 |
| デプロイ | YAML → MacroDroid 取り込み形式に変換してスマホへ配信 |
| エクスポート | スマホ上の既存マクロを PC 側の YAML に取り込む |
| バージョン管理 | シナリオ・スクリプトを Git で管理 |

**スコープ外（将来タスク）**

- ビジュアルワークフローエディタ（GUI でのフロー作成）
- 複数端末の同時管理
- MacroDroid 以外の自動化アプリ対応

### 1.3 前提条件

| 項目 | 内容 |
|------|------|
| Android | MacroDroid Pro インストール済み |
| Android | Termux インストール済み（高度なワークフロー使用時） |
| Android | クラウドストレージアプリ（Dropbox または Google Drive）インストール済み |
| PC | Node.js 環境 |
| PC | Git 環境 |
| クラウド | Dropbox または Google Drive アカウント |

---

## 2. システムアーキテクチャ

### 2.1 全体構成図

```
┌─────────────────────────────────────────────┐
│  PC                                         │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │  MacroDroid Manager App (React)      │   │
│  │  ・シナリオ管理画面                  │   │
│  │  ・YAML エディタ                     │   │
│  │  ・デプロイ操作                      │   │
│  └──────────┬───────────────────────────┘   │
│             │                               │
│  ┌──────────▼───────────────────────────┐   │
│  │  ローカル Git リポジトリ             │   │
│  │  scenarios/*.yaml                    │   │
│  │  scripts/*.sh                        │   │
│  └──────────┬───────────────────────────┘   │
│             │ deploy / export               │
│  ┌──────────▼───────────────────────────┐   │
│  │  Deploy Provider (抽象化)            │   │
│  │  └─ CloudStorageProvider             │   │
│  │     （Dropbox / Google Drive）       │   │
│  └──────────┬───────────────────────────┘   │
└────────────-│────────────────────────────────┘
              │ クラウド同期
┌─────────────▼───────────────────────────────┐
│  クラウドストレージ                          │
│  MacroDroidManager/                         │
│  ├── deploy/      PC → スマホ               │
│  │   ├── *.mdr    マクロ定義                │
│  │   └── *.sh     Termux スクリプト         │
│  └── export/      スマホ → PC              │
│      └── *.mdr    エクスポートされたマクロ  │
└─────────────┬───────────────────────────────┘
              │ クラウドアプリ自動同期
┌─────────────▼───────────────────────────────┐
│  Android スマートフォン                      │
│                                             │
│  ┌───────────────────┐  ┌────────────────┐  │
│  │  MacroDroid Pro   │  │  Termux        │  │
│  │  ・マクロ実行     │  │  ・スクリプト  │  │
│  │  ・受信マクロ     │  │    実行環境    │  │
│  │  ・ファイル監視   │  │               │  │
│  └───────────────────┘  └────────────────┘  │
│                                             │
│  ┌───────────────────────────────────────┐  │
│  │  クラウドストレージ ローカル同期フォルダ│  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

### 2.2 コンポーネント一覧

| コンポーネント | 役割 | 技術 |
|----------------|------|------|
| Manager App | PC 側管理 UI | TypeScript / React |
| YAML Converter | YAML ↔ MacroDroid 形式変換 | TypeScript |
| Deploy Provider | デプロイ方式の抽象インターフェース | TypeScript |
| CloudStorage Provider | クラウドストレージへのファイル操作 | Dropbox API / Google Drive API |
| Receiver Macro | スマホ側のファイル監視・インポート起動 | MacroDroid |
| Script Installer | Termux スクリプト配置 | MacroDroid + Shell |

---

## 3. デプロイ抽象化設計

複数のデプロイ方式を後から追加できるように、プロバイダーインターフェースで抽象化する。

### 3.1 DeployProvider インターフェース

```typescript
interface DeployProvider {
  /** シナリオ（.mdr ファイル）をデプロイ先に送信 */
  deployScenario(scenario: Scenario, mdrContent: Buffer): Promise<void>;

  /** Termux スクリプトをデプロイ先に送信 */
  deployScript(scriptName: string, content: string): Promise<void>;

  /** デプロイ先からエクスポートされた .mdr ファイルを受信 */
  listExports(): Promise<ExportEntry[]>;
  fetchExport(entry: ExportEntry): Promise<Buffer>;

  /** 接続テスト */
  testConnection(): Promise<boolean>;
}
```

### 3.2 実装プロバイダー

| プロバイダー | 状態 | 説明 |
|-------------|------|------|
| `DropboxProvider` | v1 実装対象 | Dropbox API v2 を使用 |
| `GoogleDriveProvider` | 将来追加 | Google Drive API v3 を使用 |
| `LocalNetworkProvider` | 将来追加 | LAN 内 HTTP サーバー経由 |
| `AdbProvider` | 将来追加 | ADB 経由で直接転送 |

### 3.3 プロバイダー設定

`settings.yaml`（リポジトリ管理外、`.gitignore` 対象）にプロバイダーと認証情報を記述する。

```yaml
deploy:
  provider: dropbox          # dropbox | gdrive | local | adb
  dropbox:
    accessToken: "..."
    basePath: "/MacroDroidManager"
  # gdrive:
  #   credentialsFile: "./credentials.json"
  #   basePath: "MacroDroidManager"
```

---

## 4. シナリオ YAML スキーマ

シナリオは複数のマクロをまとめた管理単位。1 ファイル = 1 シナリオ。

### 4.1 シナリオ基本構造

```yaml
apiVersion: macrodroid/v1
kind: Scenario
metadata:
  id: morning-routine          # 一意な識別子（ファイル名と一致させる）
  name: 朝のルーティン
  description: 平日の朝に実行する一連の自動化
  tags:
    - morning
    - weekday
  version: 3
  createdAt: 2026-05-01
  updatedAt: 2026-05-02

macros:
  - ...                        # マクロ定義（後述）
```

### 4.2 マクロ定義

```yaml
macros:
  - id: wake-up-wifi           # シナリオ内で一意
    name: 朝のWi-Fi有効化
    enabled: true
    category: 朝のルーティン   # MacroDroid のカテゴリ
    trigger:
      type: time
      config:
        time: "07:00"
        days: [mon, tue, wed, thu, fri]
    conditions:
      - type: battery_level
        config:
          operator: gte
          value: 20
    actions:
      - type: wifi
        config:
          state: enable
      - type: notification
        config:
          title: おはよう
          text: Wi-Fiを有効にしました
```

### 4.3 トリガー定義

| type | 説明 | config キー |
|------|------|-------------|
| `time` | 指定時刻 | `time` (HH:mm), `days` |
| `interval` | 定期実行 | `interval_minutes` |
| `location_enter` | エリア進入 | `lat`, `lng`, `radius_m` |
| `location_exit` | エリア退出 | `lat`, `lng`, `radius_m` |
| `wifi_connected` | Wi-Fi 接続 | `ssid`（省略可） |
| `wifi_disconnected` | Wi-Fi 切断 | `ssid`（省略可） |
| `screen_on` | 画面点灯 | ー |
| `screen_off` | 画面消灯 | ー |
| `battery_level` | バッテリー到達 | `level`, `direction` (above/below) |
| `webhook` | Webhook 受信 | `identifier` |
| `notification_received` | 通知受信 | `app_package` |
| `file_modified` | ファイル変更 | `path` |
| `app_launched` | アプリ起動 | `package_name` |

### 4.4 アクション定義

| type | 説明 | config キー |
|------|------|-------------|
| `notification` | 通知表示 | `title`, `text`, `priority` |
| `toast` | トースト表示 | `text` |
| `wifi` | Wi-Fi 制御 | `state` (enable/disable/toggle) |
| `bluetooth` | BT 制御 | `state` |
| `volume` | 音量設定 | `stream`, `level` |
| `launch_app` | アプリ起動 | `package_name` |
| `http_request` | HTTP リクエスト | `method`, `url`, `body`, `headers` |
| `termux_script` | Termux スクリプト実行 | `script`, `args`, `wait_for_result` |
| `set_variable` | 変数設定 | `name`, `value` |
| `if_else` | 条件分岐 | `condition`, `then_actions`, `else_actions` |
| `wait` | 待機 | `duration_ms` |
| `speak_text` | 音声読み上げ | `text` |

### 4.5 条件定義

| type | 説明 | config キー |
|------|------|-------------|
| `time_range` | 時間帯 | `from`, `to` |
| `day_of_week` | 曜日 | `days` |
| `battery_level` | バッテリー残量 | `operator`, `value` |
| `wifi_connected` | Wi-Fi 接続中 | `ssid`（省略可） |
| `screen_on` | 画面点灯中 | ー |
| `variable` | 変数値比較 | `name`, `operator`, `value` |

### 4.6 Termux スクリプト統合

```yaml
actions:
  - type: termux_script
    config:
      script: scripts/health_check.sh   # scripts/ 以下の相対パス（Git 管理）
      args: "--verbose --output /tmp/result.txt"
      wait_for_result: true             # 完了を待つか否か
      timeout_sec: 30
```

`script` フィールドはリポジトリ内の `scripts/` ディレクトリへの相対パスで指定する。デプロイ時にスクリプト本体も合わせて配信され、Termux の所定ディレクトリ（`~/macrodroid/`）に配置される。

### 4.7 サンプル：高度なワークフロー（Termux 統合）

```yaml
apiVersion: macrodroid/v1
kind: Scenario
metadata:
  id: system-health-check
  name: システムヘルスチェック
  version: 1

macros:
  - id: daily-check
    name: 日次ヘルスチェック
    enabled: true
    trigger:
      type: time
      config:
        time: "09:00"
    actions:
      - type: termux_script
        config:
          script: scripts/health_check.sh
          wait_for_result: true
          timeout_sec: 60
      - type: if_else
        config:
          condition:
            type: variable
            config:
              name: termux_exit_code
              operator: eq
              value: "0"
          then_actions:
            - type: notification
              config:
                title: ヘルスチェック
                text: "正常終了しました"
          else_actions:
            - type: http_request
              config:
                method: POST
                url: "https://example.com/alert"
                body: '{"status": "error"}'
```

---

## 5. ディレクトリ構造

### 5.1 Git リポジトリ構造

```
macrodroid-manager/
├── docs/
│   ├── 01_spec.md              # 本ファイル
│   ├── 02_tasks.md             # タスク分割
│   └── 03_adr/                 # アーキテクチャ決定記録
├── scenarios/                  # シナリオ YAML（Git 管理）
│   ├── morning-routine.yaml
│   └── system-health-check.yaml
├── scripts/                    # Termux スクリプト（Git 管理）
│   ├── health_check.sh
│   └── backup.sh
├── exports/                    # MacroDroid からエクスポートされた YAML
│   └── .gitkeep
├── app/                        # PC アプリ（TypeScript React）
│   └── ...
├── converter/                  # YAML ↔ MacroDroid 変換ライブラリ
│   └── ...
├── settings.yaml.example       # 設定ファイルのテンプレート
├── .gitignore                  # settings.yaml を除外
└── README.md
```

### 5.2 クラウドストレージ構造

```
CloudStorage/
└── MacroDroidManager/
    ├── deploy/                 # PC → スマホ
    │   ├── scenarios/
    │   │   └── {id}_{version}.mdr
    │   └── scripts/
    │       └── health_check.sh
    └── export/                 # スマホ → PC
        └── {timestamp}_{name}.mdr
```

---

## 6. デプロイフロー

### 6.1 PC → スマホ（シナリオデプロイ）

```
1. ユーザーが PC アプリでシナリオを選択して「デプロイ」
2. YAML → .mdr 変換（Converter）
3. Deploy Provider が .mdr + 関連スクリプトをクラウドストレージにアップロード
4. クラウドストレージアプリがスマホにファイルを同期
5. MacroDroid の「ファイル変更」トリガーが起動（事前セットアップ必要）
6. MacroDroid が .mdr ファイルをインポート
7. Termux スクリプトを所定のディレクトリにコピー
```

**スマホ側 セットアップマクロ（初回のみ手動設定）**

| # | 要素 | 内容 |
|---|------|------|
| トリガー | ファイル変更 | クラウド同期フォルダ内の `deploy/scenarios/` を監視 |
| アクション1 | マクロインポート | 変更されたファイルをインポート |
| アクション2 | Shell スクリプト | `deploy/scripts/` のファイルを `~/macrodroid/` にコピー |

> **注意**: MacroDroid のマクロインポートをプログラム的にトリガーする方法は実装フェーズで検証が必要。

### 6.2 スマホ → PC（エクスポート）

```
1. MacroDroid アプリから対象マクロを手動エクスポート
   → クラウドストレージの export/ フォルダに保存
2. クラウドストレージが PC に同期
3. PC アプリがエクスポートフォルダを監視し、新規 .mdr を検知
4. .mdr → YAML 変換（Converter）
5. PC アプリの「インポート確認」画面に表示
6. ユーザーが確認 → scenarios/ に保存
```

---

## 7. YAML ↔ MacroDroid 変換仕様

### 7.1 .mdr フォーマット

MacroDroid の .mdr ファイルは **ZIP アーカイブ**で、内部に `macros`（JSON、拡張子なし）が含まれる。

```json
{
  "macroList": [
    {
      "m_GUID": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "m_name": "マクロ名",
      "m_isEnabled": true,
      "m_categoryRef": "カテゴリ名",
      "m_triggerList": [ /* トリガーオブジェクト */ ],
      "m_conditionList": [ /* 条件オブジェクト */ ],
      "m_actionList": [ /* アクションオブジェクト */ ]
    }
  ],
  "categoryList": [ /* カテゴリ一覧 */ ],
  "variableList": [ /* 変数一覧 */ ]
}
```

各 trigger / condition / action は MacroDroid 内部のクラス名を持つシリアライズ形式。

### 7.2 変換方針

| 方向 | アプローチ |
|------|------------|
| YAML → .mdr | 各 type を MacroDroid 内部クラス構造にマッピングするルールを実装 |
| .mdr → YAML | 既知クラスを type にマッピング、未知クラスは `raw` として保持 |

### 7.3 変換テーブル（一部）

| YAML type | MacroDroid クラス名（要検証） |
|-----------|-------------------------------|
| `time` トリガー | `MacroDroidTimerTrigger` |
| `wifi_connected` トリガー | `MacroDroidWifiSSIDTrigger` |
| `notification` アクション | `MacroDroidNotificationAction` |
| `wifi` アクション | `MacroDroidWifiAction` |
| `termux_script` アクション | `MacroDroidRunScriptAction` |

> **要検証**: 実際の MacroDroid .mdr を複数エクスポートして内部クラス名と JSON 構造をリバースエンジニアリングする作業が必要。これは実装フェーズの最初のタスク。

---

## 8. PC アプリ仕様

### 8.1 技術スタック

| 項目 | 選択 | 理由 |
|------|------|------|
| フレームワーク | React + TypeScript | 指定 |
| ビルドツール | Vite | 高速な開発体験 |
| UI コンポーネント | shadcn/ui | 軽量・カスタマイズ容易 |
| YAML パーサー | js-yaml | メジャー・型対応 |
| コードエディタ | Monaco Editor | YAML/Shell 編集用 |
| ファイル操作 | Node.js fs API（Electron）または Web File API | デスクトップ前提なら Electron |
| クラウド API | Dropbox SDK / Google Drive API |  |
| ZIPファイル操作 | JSZip | .mdr の作成・展開 |

> **オープン**: Web アプリ（ブラウザ）か Electron（デスクトップ）かの選択が必要。ローカル Git リポジトリへのアクセスを考えると **Electron** が自然。

### 8.2 画面構成（概要）

```
┌─────────────────────────────────────────────────┐
│  MacroDroid Manager                             │
├──────────┬──────────────────────────────────────┤
│          │  [ メインコンテンツ ]                │
│ サイドバー│                                     │
│          │                                      │
│ Scenarios│                                      │
│ > 朝のルー│                                     │
│   ティン │                                      │
│ > システム│                                     │
│   ヘルス │                                      │
│          │                                      │
│ Scripts  │                                      │
│ > health │                                      │
│   _check │                                      │
│          │                                      │
│ Exports  │                                      │
│          │                                      │
│ Settings │                                      │
└──────────┴──────────────────────────────────────┘
```

| 画面 | 主な機能 |
|------|----------|
| シナリオ一覧 | 一覧表示・有効/無効切替・デプロイ・削除 |
| シナリオ詳細/編集 | YAML エディタ（Monaco）・バリデーション |
| シナリオ新規作成 | テンプレート選択 → YAML 生成 |
| スクリプト管理 | スクリプト一覧・エディタ（Shell 構文ハイライト） |
| エクスポート管理 | 受信した .mdr の確認・YAML 変換・保存 |
| 設定 | プロバイダー選択・認証情報設定・接続テスト |

---

## 9. スマホ側セットアップ

### 9.1 MacroDroid セットアップ

1. MacroDroid Pro インストール
2. クラウドストレージアプリをインストールし、`MacroDroidManager/` フォルダを同期設定
3. **受信マクロ**（手動作成）：
   - トリガー: `deploy/scenarios/` フォルダのファイル変更を監視
   - アクション: .mdr ファイルのインポート + スクリプトのコピー
4. **エクスポートマクロ**（手動作成、オプション）：
   - アクション: 指定マクロを `export/` フォルダに .mdr として保存

### 9.2 Termux セットアップ

1. Termux インストール
2. Termux:Tasker プラグインインストール（MacroDroid との連携に必要）
3. スクリプト格納ディレクトリ作成: `mkdir -p ~/macrodroid`

---

## 10. オープン事項・リスク

| # | 事項 | 優先度 | 対応方針 |
|---|------|--------|----------|
| 1 | MacroDroid .mdr の正確な内部フォーマット | 高 | 実装初期に既存マクロを複数エクスポートして解析 |
| 2 | MacroDroid でのプログラム的マクロインポート手段 | 高 | `android.intent.action.VIEW` で .mdr を開けるか検証 |
| 3 | Electron vs Web アプリの選択 | 中 | ローカル Git 操作の必要性から Electron を推奨 |
| 4 | クラウドストレージ優先実装先（Dropbox / Google Drive） | 中 | Dropbox API の方がシンプルなため Dropbox を先行 |
| 5 | Termux スクリプトのデプロイ先パス権限 | 低 | テスト時に確認 |

---

## 11. 未決定事項

- [ ] Electron vs Web (ブラウザ) の選択
- [ ] 初期対応クラウドストレージ（Dropbox 推奨）
- [ ] マクロの命名規則・ID 採番方針
- [ ] YAML バリデーションの厳格度（未知の type を許容するか）
