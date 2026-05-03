/**
 * ユーザー定義変換器のサンプル実装。
 * PlatformConverter インターフェースに従い、YAML シナリオを JSON 形式へ変換します。
 *
 * 独自の変換器を作る場合は、このファイルをコピーして platformId を変更してください。
 * `export default class` で単一クラスをエクスポートする必要があります。
 */
import type { PlatformConverter, ConvertResult, ImportResult } from '../../packages/converter/src/types/converter.js';
import type { Scenario } from '../../packages/converter/src/types/schema.js';

export default class ExampleJsonConverter implements PlatformConverter {
  readonly platformId = 'example-json';
  readonly displayName = 'Example JSON';
  readonly outputExtension = '.json';
  readonly version = '1.0.0';

  async convert(scenario: Scenario): Promise<ConvertResult> {
    const json = JSON.stringify(scenario, null, 2);
    return {
      content: Buffer.from(json, 'utf-8'),
      filename: `${scenario.metadata.id}_v${scenario.metadata.version}${this.outputExtension}`,
      warnings: ['⚠️ これはサンプル変換器です。実際の用途に合わせて実装してください。'],
    };
  }

  async importFrom(input: Buffer, _filename: string): Promise<ImportResult> {
    const scenario = JSON.parse(input.toString('utf-8')) as Scenario;
    return { scenario, warnings: [] };
  }
}
