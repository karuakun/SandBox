import { readdir, readFile, writeFile, mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import yaml from 'js-yaml';
import type { Scenario } from './types/schema.js';
import type { ConverterRegistry } from './registry.js';
import { validateScenario } from './validator.js';

export interface BuildOptions {
  scenariosDir: string;
  outputDir: string;
  registry: ConverterRegistry;
  platforms?: string[];
}

export interface BuildArtifact {
  scenarioId: string;
  platform: string;
  outputPath: string;
  filename: string;
}

export interface BuildError {
  file: string;
  platform?: string;
  message: string;
}

export interface BuildResult {
  built: BuildArtifact[];
  errors: BuildError[];
}

export async function buildScenarios(options: BuildOptions): Promise<BuildResult> {
  const { scenariosDir, outputDir, registry } = options;
  const built: BuildArtifact[] = [];
  const errors: BuildError[] = [];

  const converters = options.platforms
    ? options.platforms.map(p => registry.get(p)).filter((c): c is NonNullable<typeof c> => c != null)
    : registry.list();

  if (converters.length === 0) {
    return { built, errors: [{ file: '*', message: '変換器が登録されていません' }] };
  }

  let files: string[];
  try {
    files = (await readdir(scenariosDir)).filter(f => f.endsWith('.yaml') || f.endsWith('.yml'));
  } catch (err) {
    return { built, errors: [{ file: scenariosDir, message: `読み取り失敗: ${String(err)}` }] };
  }

  for (const file of files) {
    if (file.startsWith('_')) continue; // _system/ など内部用をスキップ

    const filePath = join(scenariosDir, file);
    let scenario: Scenario;

    try {
      const raw = await readFile(filePath, 'utf-8');
      const parsed = yaml.load(raw);
      const result = validateScenario(parsed);

      if (!result.success) {
        errors.push({ file, message: result.errors!.map(e => `${e.path}: ${e.message}`).join(', ') });
        continue;
      }
      scenario = result.data!;
    } catch (err) {
      errors.push({ file, message: `YAML 読み取り失敗: ${String(err)}` });
      continue;
    }

    for (const converter of converters) {
      try {
        const result = await converter.convert(scenario);
        const platformDir = join(outputDir, converter.platformId);
        await mkdir(platformDir, { recursive: true });
        const outputPath = join(platformDir, result.filename);
        await writeFile(outputPath, result.content);
        built.push({ scenarioId: scenario.metadata.id, platform: converter.platformId, outputPath, filename: result.filename });
      } catch (err) {
        errors.push({ file, platform: converter.platformId, message: String(err) });
      }
    }
  }

  return { built, errors };
}
