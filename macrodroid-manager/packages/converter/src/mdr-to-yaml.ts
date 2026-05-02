import type { MdrFile, MdrMacro, MdrTrigger, MdrAction, MdrConstraint } from './types/mdr.js';
import type {
  Scenario,
  MacroDefinition,
  TriggerDefinition,
  ActionDefinition,
  ConditionDefinition,
  DayOfWeek,
} from './types/schema.js';

// ─── エントリポイント ─────────────────────────────────────────────────────

export interface MdrToYamlResult {
  scenario: Scenario;
  warnings: string[];
}

export function mdrToScenario(mdr: MdrFile, scenarioId: string): MdrToYamlResult {
  const warnings: string[] = [];
  const macros: MacroDefinition[] = [];

  for (const mdrMacro of mdr.macroList) {
    const result = mdrMacroToDefinition(mdrMacro, warnings);
    if (result) macros.push(result);
  }

  const now = new Date().toISOString().split('T')[0] ?? '';
  const scenario: Scenario = {
    apiVersion: 'macrodroid/v1',
    kind: 'Scenario',
    metadata: {
      id: scenarioId,
      name: scenarioId,
      version: 1,
      createdAt: now,
      updatedAt: now,
    },
    macros,
  };

  return { scenario, warnings };
}

// ─── マクロ変換 ───────────────────────────────────────────────────────────

function mdrMacroToDefinition(mdr: MdrMacro, warnings: string[]): MacroDefinition | null {
  const triggers = mdr.m_triggerList;
  if (!triggers || triggers.length === 0) {
    warnings.push(`マクロ "${mdr.m_name}": トリガーが見つかりません。スキップします。`);
    return null;
  }

  const trigger = mdrTriggerToYaml(triggers[0]!, warnings);
  const actions = mdr.m_actionList.map((a) => mdrActionToYaml(a, warnings));
  const conditions = mdr.m_constraintList.length > 0
    ? mdr.m_constraintList.map((c) => mdrConstraintToYaml(c, warnings))
    : undefined;

  return {
    id: slugify(mdr.m_name),
    name: mdr.m_name,
    enabled: mdr.m_isEnabled,
    category: mdr.m_category || undefined,
    trigger,
    conditions,
    actions,
  };
}

// ─── トリガー変換 ─────────────────────────────────────────────────────────

function mdrTriggerToYaml(mdr: MdrTrigger, warnings: string[]): TriggerDefinition {
  switch (mdr['m_classType']) {
    case 'TimerTrigger': {
      const hour = String(mdr['m_hour'] ?? 0).padStart(2, '0');
      const minute = String(mdr['m_minute'] ?? 0).padStart(2, '0');
      const days = Array.isArray(mdr['m_daysOfWeek'])
        ? (mdr['m_daysOfWeek'] as number[]).map(indexToDay).filter((d): d is DayOfWeek => d !== null)
        : undefined;
      return { type: 'time', config: { time: `${hour}:${minute}`, days: days?.length ? days : undefined } };
    }
    case 'RegularIntervalTrigger':
      return { type: 'interval', config: { interval_minutes: Math.floor((mdr['m_seconds'] as number ?? 60) / 60) } };
    case 'WifiConnectionTrigger':
    case 'WifiConnectionTrigger2': {
      const ssid = (mdr['m_ssidList'] as string[] | undefined)?.[0];
      return (mdr['m_wifiState'] as number) === 1
        ? { type: 'wifi_connected', config: { ssid } }
        : { type: 'wifi_disconnected', config: { ssid } };
    }
    case 'ScreenOnOffTrigger':
      return mdr['m_screenOn'] ? { type: 'screen_on' } : { type: 'screen_off' };
    case 'BatteryLevelTrigger':
      return { type: 'battery_level', config: { level: mdr['m_batteryLevel'] as number, direction: mdr['m_decreasesTo'] ? 'below' : 'above' } };
    case 'WebHookTrigger':
      return { type: 'webhook', config: { identifier: mdr['m_identifier'] as string ?? '' } };
    case 'NotificationTrigger':
      return { type: 'notification_received', config: { app_package: (mdr['m_packageNameList'] as string[])?.[0], text_content: mdr['m_textContent'] as string } };
    case 'ApplicationLaunchedTrigger':
      return { type: 'app_launched', config: { package_name: (mdr['m_packageNameList'] as string[])?.[0] ?? '' } };
    case 'GeofenceTrigger':
      return mdr['m_enterArea']
        ? { type: 'location_enter', config: { lat: 0, lng: 0, radius_m: 100 } }
        : { type: 'location_exit', config: { lat: 0, lng: 0, radius_m: 100 } };
    case 'BootTrigger':
      return { type: 'boot' };
    case 'DeviceUnlockedTrigger':
      return { type: 'device_unlocked' };
    case 'EmptyTrigger':
      return { type: 'manual' };
    default:
      warnings.push(`未知のトリガー: ${mdr['m_classType']} → raw として保持します`);
      return { type: 'raw', config: { classType: mdr['m_classType'] as string, rawData: mdr as Record<string, unknown> } };
  }
}

// ─── アクション変換 ───────────────────────────────────────────────────────

function mdrActionToYaml(mdr: MdrAction, warnings: string[]): ActionDefinition {
  switch (mdr['m_classType']) {
    case 'NotificationAction':
      return { type: 'notification', config: { title: mdr['m_notificationSubject'] as string || undefined, text: mdr['m_notificationText'] as string ?? '' } };
    case 'ToastAction':
      return { type: 'toast', config: { text: mdr['m_messageText'] as string ?? '' } };
    case 'SpeakTextAction':
      return { type: 'speak_text', config: { text: mdr['m_textToSay'] as string ?? '' } };
    case 'SetWifiAction':
      return { type: 'wifi', config: { state: intToEnableState(mdr['m_state'] as number) } };
    case 'SetBluetoothAction':
      return { type: 'bluetooth', config: { state: intToEnableState(mdr['m_state'] as number) } };
    case 'SetVolumeAction': {
      const streams = mdr['m_streamIndexArray'] as number[];
      const volumes = mdr['m_streamVolumeArray'] as number[];
      return { type: 'volume', config: { stream: indexToStream(streams?.[0] ?? 3), level: volumes?.[0] ?? 50 } };
    }
    case 'LaunchActivityAction':
      return { type: 'launch_app', config: { package_name: mdr['m_packageName'] as string ?? '' } };
    case 'OpenWebPageAction':
      return { type: 'http_request', config: { method: mdr['m_httpGet'] ? 'GET' : 'POST', url: mdr['m_urlToOpen'] as string ?? '' } };
    case 'SetVariableAction': {
      const variable = mdr['m_variable'] as { m_name: string } | undefined;
      return { type: 'set_variable', config: { name: variable?.m_name ?? '', value: mdr['m_newStringValue'] as string ?? '' } };
    }
    case 'PauseAction':
      return { type: 'wait', config: { duration_ms: ((mdr['m_pauseDurationSeconds'] as number) ?? 1) * 1000 } };
    case 'DisableMacroAction':
      return { type: 'disable_macro', config: { macro_name: mdr['m_macroName'] as string ?? '', state: (mdr['m_state'] as number) === 1 ? 'enable' : 'disable' } };
    case 'ForceMacroRunAction':
      return { type: 'force_macro_run', config: { macro_name: mdr['m_macroName'] as string ?? '' } };
    default:
      warnings.push(`未知のアクション: ${mdr['m_classType']} → raw として保持します`);
      return { type: 'raw', config: { classType: mdr['m_classType'] as string, rawData: mdr as Record<string, unknown> } };
  }
}

// ─── 条件変換 ─────────────────────────────────────────────────────────────

function mdrConstraintToYaml(mdr: MdrConstraint, warnings: string[]): ConditionDefinition {
  const negate = mdr['m_isNot'] === true;
  switch (mdr['m_classType']) {
    case 'TimeOfDayConstraint':
      return {
        type: 'time_range',
        negate,
        config: {
          from: `${String(mdr['m_startHour'] ?? 0).padStart(2, '0')}:${String(mdr['m_startMinute'] ?? 0).padStart(2, '0')}`,
          to: `${String(mdr['m_endHour'] ?? 0).padStart(2, '0')}:${String(mdr['m_endMinute'] ?? 0).padStart(2, '0')}`,
        },
      };
    case 'DayOfWeekConstraint':
      return { type: 'day_of_week', negate, config: { days: ((mdr['m_daysOfWeek'] as number[]) ?? []).map(indexToDay).filter((d): d is DayOfWeek => d !== null) } };
    case 'BatteryLevelConstraint':
      return { type: 'battery_level', negate, config: { operator: mdr['m_greaterThan'] ? 'gt' : 'lt', value: mdr['m_batteryLevel'] as number ?? 0 } };
    case 'WifiConstraint':
      return { type: 'wifi_connected', negate, config: { ssid: (mdr['m_ssidList'] as string[])?.[0] } };
    case 'ScreenOnOffConstraint':
      return { type: 'screen_on', negate };
    case 'MacroDroidVariableConstraint': {
      const variable = mdr['m_variable'] as { m_name: string } | undefined;
      return {
        type: 'variable',
        negate,
        config: {
          name: variable?.m_name ?? '',
          operator: mdr['m_intGreaterThan'] ? 'gt' : mdr['m_intLessThan'] ? 'lt' : mdr['m_intNotEqual'] ? 'neq' : 'eq',
          value: mdr['m_stringEqual'] as string ?? '',
        },
      };
    }
    default:
      warnings.push(`未知の条件: ${mdr['m_classType']} → raw として保持します`);
      return { type: 'raw', config: { classType: mdr['m_classType'] as string, rawData: mdr as Record<string, unknown> } };
  }
}

// ─── ユーティリティ ───────────────────────────────────────────────────────

const INDEX_TO_DAY: DayOfWeek[] = ['sun', 'mon', 'tue', 'wed', 'thu', 'fri', 'sat'];
function indexToDay(i: number): DayOfWeek | null { return INDEX_TO_DAY[i] ?? null; }

function intToEnableState(n: number): 'enable' | 'disable' | 'toggle' {
  return n === 1 ? 'enable' : n === 0 ? 'disable' : 'toggle';
}

const INDEX_TO_STREAM: Record<number, 'ring' | 'media' | 'alarm' | 'notification' | 'system'> = {
  1: 'system', 2: 'ring', 3: 'media', 4: 'alarm', 5: 'notification',
};
function indexToStream(i: number): 'ring' | 'media' | 'alarm' | 'notification' | 'system' {
  return INDEX_TO_STREAM[i] ?? 'media';
}

function slugify(name: string): string {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
}
