import JSZip from 'jszip';
import type { Scenario } from '../../types/schema.js';
import type { PlatformConverter, ConvertResult, ImportResult } from '../../types/converter.js';
import type { MdrFile } from '../../types/mdr.js';
import { scenarioToMdr } from '../../yaml-to-mdr.js';
import { mdrToScenario } from '../../mdr-to-yaml.js';

export class MacroDroidConverter implements PlatformConverter {
  readonly platformId = 'macrodroid';
  readonly displayName = 'MacroDroid';
  readonly outputExtension = '.mdr';
  readonly version = '1.0.0';

  async convert(scenario: Scenario): Promise<ConvertResult> {
    const mdrJson = scenarioToMdr(scenario);
    const json = JSON.stringify(mdrJson, null, 2);

    // .mdr = ZIP内にmacrosというJSONファイルを持つアーカイブ（⚠️ 実機確認待ち）
    const zip = new JSZip();
    zip.file('macros', json);
    const content = Buffer.from(await zip.generateAsync({ type: 'nodebuffer' }));

    return {
      content,
      filename: `${scenario.metadata.id}_v${scenario.metadata.version}${this.outputExtension}`,
      warnings: [],
    };
  }

  async importFrom(input: Buffer, filename: string): Promise<ImportResult> {
    const scenarioId = filename
      .replace(/\.mdr$/i, '')
      .replace(/_v\d+$/, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '') || 'imported';

    let jsonText: string;
    try {
      const zip = await JSZip.loadAsync(input);
      const macrosFile = zip.file('macros');
      jsonText = macrosFile
        ? await macrosFile.async('text')
        : input.toString('utf-8');
    } catch {
      jsonText = input.toString('utf-8');
    }

    const mdrFile = JSON.parse(jsonText) as MdrFile;
    return mdrToScenario(mdrFile, scenarioId);
  }
}
