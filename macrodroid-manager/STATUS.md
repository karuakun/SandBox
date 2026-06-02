# MacroDroid Automation Manager — 実装状況

> 詳細仕様: [docs/01_spec.md](docs/01_spec.md)  
> タスク詳細: [docs/02_tasks.md](docs/02_tasks.md)  
> 最終更新: 2026-05-02

---

## Phase 0: 調査・検証

- [ ] **T0-1** MacroDroid .mdr フォーマット解析（実機必須）
- [ ] **T0-2** MacroDroid プログラム的インポート手段の検証（実機必須）
- [x] **T0-3** アプリ形態の決定 → **Electron** 確定
- [x] **T0-4** クラウドストレージ選定 → **Dropbox** 確定

---

## Phase 1: 基盤実装

### 実装タスク

- [x] **T1-1** monorepo プロジェクト構造セットアップ
- [x] **T1-2** YAML スキーマの TypeScript 型定義
- [x] **T1-3** YAML バリデーター実装（zod）
- [x] **T1-4** YAML → .mdr 変換ロジック実装
- [x] **T1-5** .mdr → YAML 変換ロジック実装
- [x] **T1-6** DeployProvider インターフェース定義
- [x] **T1-7** DropboxProvider 実装
- [ ] **T1-8** PlatformConverter インターフェース + ConverterRegistry
- [ ] **T1-9** MacroDroidConverter クラスへリファクタリング
- [ ] **T1-10** buildScenarios コマンド + dist/ Git 管理セットアップ
- [ ] **T1-11** ユーザー定義変換器サンプル + 動的ロード
- [ ] **T1-12** Claude API 変換器生成ツール（`tools/create-converter.ts`）

### 単体テスト

- [ ] **T1-13** テスト環境セットアップ（vitest）
- [ ] **T1-14** validator 単体テスト
- [ ] **T1-15** yaml-to-mdr 単体テスト
- [ ] **T1-16** mdr-to-yaml 単体テスト
- [ ] **T1-17** MacroDroidConverter 単体テスト（T1-9 後）
- [ ] **T1-18** ConverterRegistry 単体テスト（T1-8 後）
- [ ] **T1-19** DropboxProvider 単体テスト（モック）

### 結合テスト

- [ ] **T1-20** YAML → .mdr → YAML ラウンドトリップ結合テスト（T1-9 後）
- [ ] **T1-21** buildScenarios 結合テスト（T1-10 後）
- [ ] **T1-22** ConverterRegistry + ユーザー定義変換器 結合テスト（T1-11 後）

---

## Phase 2: PC アプリ実装

- [ ] **T2-1** Vite + React + TypeScript + Electron 初期化
- [ ] **T2-2** レイアウト・ナビゲーション
- [ ] **T2-3** シナリオ一覧画面
- [ ] **T2-4** シナリオ編集画面（YAML エディタ）
- [ ] **T2-5** シナリオ新規作成
- [ ] **T2-6** スクリプト管理画面
- [ ] **T2-7** エクスポート管理画面
- [ ] **T2-8** コンバーター管理画面（AI 生成フォーム含む）
- [ ] **T2-9** 設定画面
- [ ] **T2-10** ビルド・デプロイ機能の結合

---

## Phase 3: スマホ連携整備

- [ ] **T3-1** スマホ側セットアップガイド作成（`docs/05_phone_setup.md`）
- [ ] **T3-2** 受信マクロのテンプレート作成
- [ ] **T3-3** E2E テスト
- [ ] **T3-4** 実機 .mdr ファイルを使った変換テスト（T0-1 後）

---

## Phase 4: ビジュアルエディタ（将来）

- [ ] ノードベース GUI ワークフローエディタ
- [ ] YAML との双方向同期

---

## 進捗サマリー

| フェーズ | 完了 | 残り | 合計 |
|--------|------|------|------|
| Phase 0 | 2 | 2 | 4 |
| Phase 1 実装 | 7 | 5 | 12 |
| Phase 1 テスト | 0 | 10 | 10 |
| Phase 2 | 0 | 10 | 10 |
| Phase 3 | 0 | 4 | 4 |
| **合計** | **9** | **31** | **40** |
