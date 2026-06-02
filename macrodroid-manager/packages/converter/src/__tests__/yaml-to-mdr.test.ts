import { describe, it, expect } from 'vitest';
import { scenarioToMdr } from '../yaml-to-mdr.js';
import type { Scenario } from '../types/schema.js';

function makeScenario(overrides: Partial<Scenario['macros'][number]> = {}): Scenario {
  return {
    apiVersion: 'macrodroid/v1',
    kind: 'Scenario',
    metadata: {
      id: 'test',
      name: 'Test',
      version: 1,
      createdAt: '2024-01-01',
      updatedAt: '2024-01-01',
    },
    macros: [
      {
        id: 'macro1',
        name: 'Macro One',
        enabled: true,
        trigger: { type: 'manual' },
        actions: [{ type: 'toast', config: { text: 'hi' } }],
        ...overrides,
      },
    ],
  };
}

describe('scenarioToMdr', () => {
  it('macroList と categoryList を返す', () => {
    const mdr = scenarioToMdr(makeScenario());
    expect(mdr.macroList).toHaveLength(1);
    expect(mdr.categoryList).toEqual([]);
    expect(mdr.variableList).toEqual([]);
  });

  it('マクロ名と enabled フラグを保持する', () => {
    const mdr = scenarioToMdr(makeScenario({ name: 'Morning', enabled: false }));
    expect(mdr.macroList[0]!.m_name).toBe('Morning');
    expect(mdr.macroList[0]!.m_isEnabled).toBe(false);
  });

  it('カテゴリを重複排除して抽出する', () => {
    const scenario: Scenario = {
      ...makeScenario(),
      macros: [
        { id: 'a', name: 'A', enabled: true, trigger: { type: 'manual' }, actions: [{ type: 'toast', config: { text: 'x' } }], category: 'Work' },
        { id: 'b', name: 'B', enabled: true, trigger: { type: 'manual' }, actions: [{ type: 'toast', config: { text: 'y' } }], category: 'Work' },
        { id: 'c', name: 'C', enabled: true, trigger: { type: 'manual' }, actions: [{ type: 'toast', config: { text: 'z' } }], category: 'Home' },
      ],
    };
    const mdr = scenarioToMdr(scenario);
    expect(mdr.categoryList).toHaveLength(2);
    expect(mdr.categoryList.map((c) => c.m_name)).toContain('Work');
    expect(mdr.categoryList.map((c) => c.m_name)).toContain('Home');
  });

  describe('トリガー変換', () => {
    it('time → TimerTrigger', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'time', config: { time: '07:30' } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger.m_classType).toBe('TimerTrigger');
      expect(trigger['m_hour']).toBe(7);
      expect(trigger['m_minute']).toBe(30);
    });

    it('time に days を指定するとインデックス配列になる', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'time', config: { time: '09:00', days: ['mon', 'fri'] } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger['m_daysOfWeek']).toEqual([1, 5]); // mon=1, fri=5
    });

    it('interval → RegularIntervalTrigger（分 → 秒）', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'interval', config: { interval_minutes: 15 } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger.m_classType).toBe('RegularIntervalTrigger');
      expect(trigger['m_seconds']).toBe(900);
    });

    it('wifi_connected → WifiConnectionTrigger (state=1)', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'wifi_connected', config: { ssid: 'MyNet' } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger.m_classType).toBe('WifiConnectionTrigger');
      expect(trigger['m_wifiState']).toBe(1);
      expect(trigger['m_ssidList']).toEqual(['MyNet']);
    });

    it('wifi_disconnected → WifiConnectionTrigger (state=0)', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'wifi_disconnected' } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger['m_wifiState']).toBe(0);
    });

    it('screen_on → ScreenOnOffTrigger', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'screen_on' } }));
      expect(mdr.macroList[0]!.m_triggerList[0]!.m_classType).toBe('ScreenOnOffTrigger');
      expect(mdr.macroList[0]!.m_triggerList[0]!['m_screenOn']).toBe(true);
    });

    it('battery_level → BatteryLevelTrigger', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'battery_level', config: { level: 20, direction: 'below' } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger.m_classType).toBe('BatteryLevelTrigger');
      expect(trigger['m_batteryLevel']).toBe(20);
      expect(trigger['m_decreasesTo']).toBe(true);
    });

    it('webhook → WebHookTrigger', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'webhook', config: { identifier: 'my-hook' } } }));
      const trigger = mdr.macroList[0]!.m_triggerList[0]!;
      expect(trigger.m_classType).toBe('WebHookTrigger');
      expect(trigger['m_identifier']).toBe('my-hook');
    });

    it('boot → BootTrigger', () => {
      const mdr = scenarioToMdr(makeScenario({ trigger: { type: 'boot' } }));
      expect(mdr.macroList[0]!.m_triggerList[0]!.m_classType).toBe('BootTrigger');
    });

    it('manual → EmptyTrigger', () => {
      const mdr = scenarioToMdr(makeScenario());
      expect(mdr.macroList[0]!.m_triggerList[0]!.m_classType).toBe('EmptyTrigger');
    });
  });

  describe('アクション変換', () => {
    it('toast → ToastAction', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'toast', config: { text: 'Hello', duration: 'long' } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('ToastAction');
      expect(action['m_messageText']).toBe('Hello');
      expect(action['m_duration']).toBe(1);
    });

    it('notification → NotificationAction', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'notification', config: { text: 'Alert', priority: 'high' } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('NotificationAction');
      expect(action['m_priority']).toBe(2);
    });

    it('volume → SetVolumeAction（stream インデックス）', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'volume', config: { stream: 'ring', level: 80 } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('SetVolumeAction');
      expect(action['m_streamIndexArray']).toEqual([2]); // ring=2
      expect(action['m_streamVolumeArray']).toEqual([80]);
    });

    it('wifi → SetWifiAction', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'wifi', config: { state: 'toggle' } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('SetWifiAction');
      expect(action['m_state']).toBe(2); // toggle=2
    });

    it('wait → PauseAction（ms → 秒）', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'wait', config: { duration_ms: 5000 } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('PauseAction');
      expect(action['m_pauseDurationSeconds']).toBe(5);
    });

    it('termux_script → RunScriptAction', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'termux_script', config: { script: 'scripts/run.sh', args: '--debug' } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('RunScriptAction');
      expect(action['m_scriptText']).toContain('run.sh');
    });

    it('http_request → OpenWebPageAction', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'http_request', config: { method: 'POST', url: 'https://example.com' } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('OpenWebPageAction');
      expect(action['m_httpGet']).toBe(false);
    });

    it('raw → classType そのまま', () => {
      const mdr = scenarioToMdr(makeScenario({ actions: [{ type: 'raw', config: { classType: 'CustomAction', rawData: { foo: 'bar' } } }] }));
      const action = mdr.macroList[0]!.m_actionList[0]!;
      expect(action.m_classType).toBe('CustomAction');
      expect(action['foo']).toBe('bar');
    });
  });

  describe('条件変換', () => {
    it('time_range → TimeOfDayConstraint', () => {
      const mdr = scenarioToMdr(makeScenario({
        conditions: [{ type: 'time_range', config: { from: '08:00', to: '20:00' } }],
      }));
      const constraint = mdr.macroList[0]!.m_constraintList[0]!;
      expect(constraint.m_classType).toBe('TimeOfDayConstraint');
      expect(constraint['m_startHour']).toBe(8);
      expect(constraint['m_endHour']).toBe(20);
    });

    it('negate フラグが m_isNot に変換される', () => {
      const mdr = scenarioToMdr(makeScenario({
        conditions: [{ type: 'screen_on', negate: true }],
      }));
      expect(mdr.macroList[0]!.m_constraintList[0]!['m_isNot']).toBe(true);
    });

    it('day_of_week → DayOfWeekConstraint', () => {
      const mdr = scenarioToMdr(makeScenario({
        conditions: [{ type: 'day_of_week', config: { days: ['sat', 'sun'] } }],
      }));
      const constraint = mdr.macroList[0]!.m_constraintList[0]!;
      expect(constraint.m_classType).toBe('DayOfWeekConstraint');
      expect(constraint['m_daysOfWeek']).toEqual([6, 0]); // sat=6, sun=0
    });
  });
});
