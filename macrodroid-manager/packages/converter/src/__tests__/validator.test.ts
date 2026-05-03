import { describe, it, expect } from 'vitest';
import { validateScenario } from '../validator.js';

const minimalScenario = {
  apiVersion: 'macrodroid/v1',
  kind: 'Scenario',
  metadata: {
    id: 'test-scenario',
    name: 'Test Scenario',
    version: 1,
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
  },
  macros: [
    {
      id: 'test-macro',
      name: 'Test Macro',
      enabled: true,
      trigger: { type: 'manual' },
      actions: [{ type: 'toast', config: { text: 'hello' } }],
    },
  ],
};

describe('validateScenario', () => {
  it('最小限の有効なシナリオを受け入れる', () => {
    const result = validateScenario(minimalScenario);
    expect(result.success).toBe(true);
    expect(result.data).toBeDefined();
    expect(result.errors).toBeUndefined();
  });

  it('apiVersion が不正なら失敗する', () => {
    const result = validateScenario({ ...minimalScenario, apiVersion: 'v1' });
    expect(result.success).toBe(false);
    expect(result.errors).toBeDefined();
    expect(result.errors!.length).toBeGreaterThan(0);
  });

  it('kind が不正なら失敗する', () => {
    const result = validateScenario({ ...minimalScenario, kind: 'Macro' });
    expect(result.success).toBe(false);
  });

  it('metadata.id に大文字が含まれると失敗する', () => {
    const result = validateScenario({
      ...minimalScenario,
      metadata: { ...minimalScenario.metadata, id: 'Test-Scenario' },
    });
    expect(result.success).toBe(false);
  });

  it('macros が空配列なら失敗する', () => {
    const result = validateScenario({ ...minimalScenario, macros: [] });
    expect(result.success).toBe(false);
  });

  it('actions が空配列なら失敗する', () => {
    const badMacro = { ...minimalScenario.macros[0], actions: [] };
    const result = validateScenario({ ...minimalScenario, macros: [badMacro] });
    expect(result.success).toBe(false);
  });

  it('time トリガーの HH:mm 形式を検証する', () => {
    const validMacro = {
      ...minimalScenario.macros[0],
      trigger: { type: 'time', config: { time: '07:30' } },
    };
    const result = validateScenario({ ...minimalScenario, macros: [validMacro] });
    expect(result.success).toBe(true);
  });

  it('time トリガーの不正な時刻形式を拒否する', () => {
    const badMacro = {
      ...minimalScenario.macros[0],
      trigger: { type: 'time', config: { time: '7:30' } },
    };
    const result = validateScenario({ ...minimalScenario, macros: [badMacro] });
    expect(result.success).toBe(false);
  });

  it('battery_level 条件の値域を検証する', () => {
    const macroWithCondition = {
      ...minimalScenario.macros[0],
      conditions: [
        { type: 'battery_level', config: { operator: 'lt', value: 20 } },
      ],
    };
    const result = validateScenario({ ...minimalScenario, macros: [macroWithCondition] });
    expect(result.success).toBe(true);
  });

  it('battery_level 条件が範囲外なら失敗する', () => {
    const macroWithCondition = {
      ...minimalScenario.macros[0],
      conditions: [
        { type: 'battery_level', config: { operator: 'lt', value: 101 } },
      ],
    };
    const result = validateScenario({ ...minimalScenario, macros: [macroWithCondition] });
    expect(result.success).toBe(false);
  });

  it('http_request の URL を検証する', () => {
    const macroWithHttp = {
      ...minimalScenario.macros[0],
      actions: [
        { type: 'http_request', config: { method: 'GET', url: 'not-a-url' } },
      ],
    };
    const result = validateScenario({ ...minimalScenario, macros: [macroWithHttp] });
    expect(result.success).toBe(false);
  });

  it('termux_script の scripts/ プレフィックスを検証する', () => {
    const validScript = {
      ...minimalScenario.macros[0],
      actions: [{ type: 'termux_script', config: { script: 'scripts/run.sh' } }],
    };
    expect(validateScenario({ ...minimalScenario, macros: [validScript] }).success).toBe(true);

    const invalidScript = {
      ...minimalScenario.macros[0],
      actions: [{ type: 'termux_script', config: { script: 'run.sh' } }],
    };
    expect(validateScenario({ ...minimalScenario, macros: [invalidScript] }).success).toBe(false);
  });

  it('エラーに path と message が含まれる', () => {
    const result = validateScenario({ apiVersion: 'macrodroid/v1', kind: 'Scenario' });
    expect(result.success).toBe(false);
    expect(result.errors![0]).toHaveProperty('path');
    expect(result.errors![0]).toHaveProperty('message');
  });

  it('null を渡すと失敗する', () => {
    const result = validateScenario(null);
    expect(result.success).toBe(false);
  });
});
