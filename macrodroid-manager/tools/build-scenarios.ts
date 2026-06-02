#!/usr/bin/env tsx
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ConverterRegistry, MacroDroidConverter, buildScenarios } from '../packages/converter/src/index.js';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const root = resolve(__dirname, '..');

const registry = new ConverterRegistry();
registry.register(new MacroDroidConverter());

// converters/ ディレクトリのユーザー定義変換器を動的ロード
const convertersDir = resolve(root, 'converters');
const { loaded, errors: loadErrors } = await registry.loadFromDirectory(convertersDir);
if (loaded.length > 0) {
  console.log(`変換器をロード: ${loaded.join(', ')}`);
}
for (const e of loadErrors) {
  console.warn(`⚠️  変換器ロード失敗: ${e}`);
}

const result = await buildScenarios({
  scenariosDir: resolve(root, 'scenarios'),
  outputDir: resolve(root, 'dist'),
  registry,
});

for (const artifact of result.built) {
  console.log(`✅ ${artifact.scenarioId} → ${artifact.outputPath}`);
}

for (const err of result.errors) {
  const platform = err.platform ? ` [${err.platform}]` : '';
  console.error(`❌ ${err.file}${platform}: ${err.message}`);
}

if (result.errors.length > 0) {
  process.exit(1);
}
