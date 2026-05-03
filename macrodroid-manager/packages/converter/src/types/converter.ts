import type { Scenario } from './schema.js';

export interface ConvertResult {
  content: Buffer;
  filename: string;
  warnings: string[];
}

export interface ImportResult {
  scenario: Scenario;
  warnings: string[];
}

/**
 * プラットフォーム変換器の抽象インターフェース。
 * MacroDroid以外のプラットフォーム（Tasker等）対応時はこれを実装する。
 */
export interface PlatformConverter {
  readonly platformId: string;       // 'macrodroid', 'tasker' など
  readonly displayName: string;
  readonly outputExtension: string;  // '.mdr', '.xml' など
  readonly version: string;

  convert(scenario: Scenario): Promise<ConvertResult>;
  importFrom?(input: Buffer, filename: string): Promise<ImportResult>;
}
