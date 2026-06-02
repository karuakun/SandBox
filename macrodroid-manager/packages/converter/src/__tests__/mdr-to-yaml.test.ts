import { describe, it, expect } from 'vitest';
import { mdrToScenario } from '../mdr-to-yaml.js';
import type { MdrFile } from '../types/mdr.js';

function makeMdr(overrides: Partial<MdrFile['macroList'][number]> = {}): MdrFile {
  return {
    macroList: [
      {
        m_GUID: 12345,
        m_name: 'Test Macro',
        m_description: '',
        m_isEnabled: true,
        m_category: '',
        m_triggerList: [{ m_classType: 'EmptyTrigger' }],
        m_actionList: [{ m_classType: 'ToastAction', m_messageText: 'hi', m_imageResourceName: '', m_duration: 0, m_position: 1 }],
        m_constraintList: [],
        m_isOrCondition: false,
        localVariables: [],
        ...overrides,
      },
    ],
    categoryList: [],
    variableList: [],
  };
}

describe('mdrToScenario', () => {
  it('Scenario 構造を返す', () => {
    const { scenario, warnings } = mdrToScenario(makeMdr(), 'my-scenario');
    expect(scenario.apiVersion).toBe('macrodroid/v1');
    expect(scenario.kind).toBe('Scenario');
    expect(scenario.metadata.id).toBe('my-scenario');
    expect(warnings).toHaveLength(0);
  });

  it('マクロ名と enabled を保持する', () => {
    const { scenario } = mdrToScenario(makeMdr({ m_name: 'Night Mode', m_isEnabled: false }), 'test');
    expect(scenario.macros[0]!.name).toBe('Night Mode');
    expect(scenario.macros[0]!.enabled).toBe(false);
  });

  it('category が空文字なら undefined になる', () => {
    const { scenario } = mdrToScenario(makeMdr({ m_category: '' }), 'test');
    expect(scenario.macros[0]!.category).toBeUndefined();
  });

  it('category が空でない場合は保持される', () => {
    const { scenario } = mdrToScenario(makeMdr({ m_category: 'Work' }), 'test');
    expect(scenario.macros[0]!.category).toBe('Work');
  });

  it('トリガーのないマクロはスキップして警告を出す', () => {
    const mdr = makeMdr({ m_triggerList: [] });
    const { scenario, warnings } = mdrToScenario(mdr, 'test');
    expect(scenario.macros).toHaveLength(0);
    expect(warnings).toHaveLength(1);
  });

  describe('トリガー変換', () => {
    it('EmptyTrigger → manual', () => {
      const { scenario } = mdrToScenario(makeMdr(), 'test');
      expect(scenario.macros[0]!.trigger.type).toBe('manual');
    });

    it('TimerTrigger → time（HH:mm 形式）', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'TimerTrigger', m_hour: 7, m_minute: 5, m_daysOfWeek: [] }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      const trigger = scenario.macros[0]!.trigger;
      expect(trigger.type).toBe('time');
      if (trigger.type === 'time') {
        expect(trigger.config.time).toBe('07:05');
      }
    });

    it('RegularIntervalTrigger → interval（秒 → 分）', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'RegularIntervalTrigger', m_seconds: 1800 }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      const trigger = scenario.macros[0]!.trigger;
      expect(trigger.type).toBe('interval');
      if (trigger.type === 'interval') {
        expect(trigger.config.interval_minutes).toBe(30);
      }
    });

    it('WifiConnectionTrigger (state=1) → wifi_connected', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'WifiConnectionTrigger', m_wifiState: 1, m_ssidList: ['MyNet'] }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.trigger.type).toBe('wifi_connected');
    });

    it('WifiConnectionTrigger (state=0) → wifi_disconnected', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'WifiConnectionTrigger', m_wifiState: 0, m_ssidList: [] }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.trigger.type).toBe('wifi_disconnected');
    });

    it('BootTrigger → boot', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'BootTrigger' }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.trigger.type).toBe('boot');
    });

    it('未知のトリガー → raw + 警告', () => {
      const mdr = makeMdr({ m_triggerList: [{ m_classType: 'UnknownTrigger' }] });
      const { scenario, warnings } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.trigger.type).toBe('raw');
      expect(warnings.some((w) => w.includes('UnknownTrigger'))).toBe(true);
    });
  });

  describe('アクション変換', () => {
    it('ToastAction → toast', () => {
      const { scenario } = mdrToScenario(makeMdr(), 'test');
      expect(scenario.macros[0]!.actions[0]!.type).toBe('toast');
    });

    it('NotificationAction → notification', () => {
      const mdr = makeMdr({ m_actionList: [{ m_classType: 'NotificationAction', m_notificationSubject: 'Title', m_notificationText: 'Body' }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      const action = scenario.macros[0]!.actions[0]!;
      expect(action.type).toBe('notification');
      if (action.type === 'notification') {
        expect(action.config.text).toBe('Body');
      }
    });

    it('SetVolumeAction → volume（インデックス → stream 名）', () => {
      const mdr = makeMdr({ m_actionList: [{ m_classType: 'SetVolumeAction', m_streamIndexArray: [3], m_streamVolumeArray: [70] }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      const action = scenario.macros[0]!.actions[0]!;
      expect(action.type).toBe('volume');
      if (action.type === 'volume') {
        expect(action.config.stream).toBe('media'); // 3=media
        expect(action.config.level).toBe(70);
      }
    });

    it('PauseAction → wait（秒 → ms）', () => {
      const mdr = makeMdr({ m_actionList: [{ m_classType: 'PauseAction', m_pauseDurationSeconds: 3 }] });
      const { scenario } = mdrToScenario(mdr, 'test');
      const action = scenario.macros[0]!.actions[0]!;
      expect(action.type).toBe('wait');
      if (action.type === 'wait') {
        expect(action.config.duration_ms).toBe(3000);
      }
    });

    it('未知のアクション → raw + 警告', () => {
      const mdr = makeMdr({ m_actionList: [{ m_classType: 'UnknownAction' }] });
      const { scenario, warnings } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.actions[0]!.type).toBe('raw');
      expect(warnings.some((w) => w.includes('UnknownAction'))).toBe(true);
    });
  });

  describe('条件変換', () => {
    it('TimeOfDayConstraint → time_range', () => {
      const mdr = makeMdr({
        m_constraintList: [{ m_classType: 'TimeOfDayConstraint', m_startHour: 8, m_startMinute: 0, m_endHour: 22, m_endMinute: 30, m_isNot: false }],
      });
      const { scenario } = mdrToScenario(mdr, 'test');
      const condition = scenario.macros[0]!.conditions![0]!;
      expect(condition.type).toBe('time_range');
      if (condition.type === 'time_range') {
        expect(condition.config.from).toBe('08:00');
        expect(condition.config.to).toBe('22:30');
      }
    });

    it('m_isNot=true → negate: true', () => {
      const mdr = makeMdr({
        m_constraintList: [{ m_classType: 'ScreenOnOffConstraint', m_screenOn: true, m_isNot: true }],
      });
      const { scenario } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.conditions![0]!.negate).toBe(true);
    });

    it('未知の条件 → raw + 警告', () => {
      const mdr = makeMdr({
        m_constraintList: [{ m_classType: 'UnknownConstraint' }],
      });
      const { scenario, warnings } = mdrToScenario(mdr, 'test');
      expect(scenario.macros[0]!.conditions![0]!.type).toBe('raw');
      expect(warnings.some((w) => w.includes('UnknownConstraint'))).toBe(true);
    });
  });
});
