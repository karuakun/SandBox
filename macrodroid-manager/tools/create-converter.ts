#!/usr/bin/env tsx
/**
 * AI でプラットフォーム変換器のスケルトンを生成するツール。
 * Usage: tsx tools/create-converter.ts <platformId> "<description>"
 * Example: tsx tools/create-converter.ts tasker "Tasker XML format for Android automation"
 */
import Anthropic from '@anthropic-ai/sdk';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const root = resolve(__dirname, '..');

const [platformId, description] = process.argv.slice(2);

if (!platformId || !description) {
  console.error('Usage: tsx tools/create-converter.ts <platformId> "<description>"');
  console.error('Example: tsx tools/create-converter.ts tasker "Tasker XML format"');
  process.exit(1);
}

if (!/^[a-z0-9-]+$/.test(platformId)) {
  console.error('platformId は小文字英数字とハイフンのみ使用できます');
  process.exit(1);
}

const converterTypesPath = resolve(root, 'packages/converter/src/types/converter.ts');
const schemaTypesPath = resolve(root, 'packages/converter/src/types/schema.ts');
const exampleConverterPath = resolve(root, 'converters/example/index.ts');

const [converterTypes, schemaTypes, exampleConverter] = await Promise.all([
  readFile(converterTypesPath, 'utf-8'),
  readFile(schemaTypesPath, 'utf-8'),
  readFile(exampleConverterPath, 'utf-8'),
]);

const client = new Anthropic();

const systemPrompt = `あなたはMacroDroid Automation ManagerのPlatformConverter実装を生成する専門家です。

以下の型定義に厳密に従い、TypeScriptコードを生成してください。

## PlatformConverter インターフェース定義
\`\`\`typescript
${converterTypes}
\`\`\`

## Scenario スキーマ型定義
\`\`\`typescript
${schemaTypes}
\`\`\`

## サンプル変換器（参考実装）
\`\`\`typescript
${exampleConverter}
\`\`\`

## 生成ルール
- \`export default class\` で単一クラスをエクスポートすること
- \`PlatformConverter\` インターフェースを implements すること
- \`platformId\`、\`displayName\`、\`outputExtension\`、\`version\` を適切に設定すること
- インポートパスは \`../../packages/converter/src/types/converter.js\` と \`../../packages/converter/src/types/schema.js\` を使うこと
- \`convert()\` メソッドは実際の変換ロジックの骨格を実装すること
- \`importFrom()\` メソッドも実装すること
- コメントは日本語で書くこと
- TypeScript strict mode に対応すること（exactOptionalPropertyTypes含む）
- コードブロックのマークダウンなしで、TypeScriptコードのみ出力すること`;

console.log(`🤖 ${platformId} 変換器を生成中...`);

let generatedCode = '';

const stream = await client.messages.stream({
  model: 'claude-opus-4-7',
  max_tokens: 4096,
  thinking: { type: 'adaptive' },
  system: [
    {
      type: 'text',
      text: systemPrompt,
      cache_control: { type: 'ephemeral' },
    },
  ],
  messages: [
    {
      role: 'user',
      content: `プラットフォームID「${platformId}」向けの変換器を生成してください。\n\n説明: ${description}\n\nTypeScriptコードのみ出力してください（マークダウンのコードブロックなし）。`,
    },
  ],
});

process.stdout.write('\n');

for await (const chunk of stream) {
  if (chunk.type === 'content_block_delta' && chunk.delta.type === 'text_delta') {
    process.stdout.write(chunk.delta.text);
    generatedCode += chunk.delta.text;
  }
}

process.stdout.write('\n\n');

// コードブロックのフェンスが含まれていた場合はクリーンアップ
const codeMatch = generatedCode.match(/```(?:typescript|ts)?\n([\s\S]*?)```/);
if (codeMatch?.[1]) {
  generatedCode = codeMatch[1];
}

const outputDir = join(root, 'converters', platformId);
const outputPath = join(outputDir, 'index.ts');

await mkdir(outputDir, { recursive: true });
await writeFile(outputPath, generatedCode, 'utf-8');

console.log(`✅ 変換器を保存しました: converters/${platformId}/index.ts`);
console.log('💡 tsx でビルドするには: npm run build:scenarios');
