import type { Scenario } from '@macrodroid-manager/converter';

export interface ExportEntry {
  id: string;
  filename: string;
  uploadedAt: string;
  sizeBytes: number;
}

export interface DeployResult {
  success: boolean;
  deployedAt: string;
  provider: string;
  error?: string;
}

/**
 * デプロイ方式の抽象インターフェース。
 * 新しいデプロイ先（Google Drive、ADB等）は本インターフェースを実装して追加する。
 */
export interface DeployProvider {
  readonly name: string;

  /** 接続テスト。成功したら true */
  testConnection(): Promise<boolean>;

  /** シナリオ（.mdr バッファ）をデプロイ先に送信 */
  deployScenario(scenario: Scenario, mdrContent: Buffer): Promise<DeployResult>;

  /** Termux スクリプトをデプロイ先に送信 */
  deployScript(scriptName: string, content: string): Promise<DeployResult>;

  /** デプロイ先からエクスポートされた .mdr ファイルの一覧を取得 */
  listExports(): Promise<ExportEntry[]>;

  /** エクスポートされた .mdr ファイルを取得 */
  fetchExport(entry: ExportEntry): Promise<Buffer>;

  /** エクスポートされた .mdr ファイルを削除（取り込み後のクリーンアップ） */
  deleteExport(entry: ExportEntry): Promise<void>;
}

export interface ProviderConfig {
  provider: 'dropbox' | 'gdrive' | 'local' | 'adb';
  dropbox?: DropboxProviderConfig;
  gdrive?: GDriveProviderConfig;
  local?: LocalProviderConfig;
}

export interface DropboxProviderConfig {
  accessToken: string;
  basePath: string;           // 例: /MacroDroidManager
}

export interface GDriveProviderConfig {
  credentialsFile: string;
  basePath: string;
}

export interface LocalProviderConfig {
  basePath: string;           // ローカルファイルシステムのパス
}
