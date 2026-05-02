import type {
  Scenario,
  MacroDefinition,
  TriggerDefinition,
  ActionDefinition,
  ConditionDefinition,
} from './types/schema.js';
import type {
  MdrFile,
  MdrMacro,
  MdrTrigger,
  MdrAction,
  MdrConstraint,
} from './types/mdr.js';

// ─── エントリポイント ─────────────────────────────────────────────────────

export function scenarioToMdr(scenario: Scenario): MdrFile {
  return {
    macroList: scenario.macros.map((macro) => macroToMdr(macro)),
    categoryList: extractCategories(scenario),
    variableList: [],
  };
}

// ─── マクロ変換 ───────────────────────────────────────────────────────────

function macroToMdr(macro: MacroDefinition): MdrMacro {
  return {
    m_GUID: generateGuid(),
    m_name: macro.name,
    m_description: '',
    m_isEnabled: macro.enabled,
    m_category: macro.category ?? '',
    m_triggerList: [triggerToMdr(macro.trigger)],
    m_actionList: macro.actions.map(actionToMdr),
    m_constraintList: (macro.conditions ?? []).map(conditionToMdr),
    m_isOrCondition: false,
    localVariables: [],
  };
}

// ─── トリガー変換 ─────────────────────────────────────────────────────────

function triggerToMdr(trigger: TriggerDefinition): MdrTrigger {
  switch (trigger.type) {
    case 'time':
      return {
        m_classType: 'TimerTrigger',
        m_daysOfWeek: (trigger.config.days ?? ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun']).map(dayToIndex),
        m_hour: parseInt(trigger.config.time.split(':')[0]!, 10),
        m_minute: parseInt(trigger.config.time.split(':')[1]!, 10),
      };

    case 'interval':
      return {
        m_classType: 'RegularIntervalTrigger',
        m_seconds: trigger.config.interval_minutes * 60,
        m_startHour: trigger.config.start_hour ?? 0,
      };

    case 'wifi_connected':
      return {
        m_classType: 'WifiConnectionTrigger',
        m_ssidList: trigger.config?.ssid ? [trigger.config.ssid] : [],
        m_wifiState: 1,
      };

    case 'wifi_disconnected':
      return {
        m_classType: 'WifiConnectionTrigger',
        m_ssidList: trigger.config?.ssid ? [trigger.config.ssid] : [],
        m_wifiState: 0,
      };

    case 'screen_on':
      return { m_classType: 'ScreenOnOffTrigger', m_screenOn: true };

    case 'screen_off':
      return { m_classType: 'ScreenOnOffTrigger', m_screenOn: false };

    case 'battery_level':
      return {
        m_classType: 'BatteryLevelTrigger',
        m_batteryLevel: trigger.config.level,
        m_decreasesTo: trigger.config.direction === 'below',
      };

    case 'webhook':
      return {
        m_classType: 'WebHookTrigger',
        m_identifier: trigger.config.identifier,
        m_url: '',
      };

    case 'notification_received':
      return {
        m_classType: 'NotificationTrigger',
        m_textContent: trigger.config?.text_content ?? '',
        m_packageNameList: trigger.config?.app_package ? [trigger.config.app_package] : [],
      };

    case 'app_launched':
      return {
        m_classType: 'ApplicationLaunchedTrigger',
        m_applicationNameList: [],
        m_packageNameList: [trigger.config.package_name],
        m_launched: true,
      };

    case 'location_enter':
      return {
        m_classType: 'GeofenceTrigger',
        m_geofenceId: generateGuid(),
        m_enterArea: true,
      };

    case 'location_exit':
      return {
        m_classType: 'GeofenceTrigger',
        m_geofenceId: generateGuid(),
        m_enterArea: false,
      };

    case 'boot':
      return { m_classType: 'BootTrigger' };

    case 'device_unlocked':
      return { m_classType: 'DeviceUnlockedTrigger' };

    case 'manual':
      return { m_classType: 'EmptyTrigger' };

    case 'raw':
      return { m_classType: trigger.config.classType, ...trigger.config.rawData };
  }
}

// ─── アクション変換 ───────────────────────────────────────────────────────

function actionToMdr(action: ActionDefinition): MdrAction {
  switch (action.type) {
    case 'notification':
      return {
        m_classType: 'NotificationAction',
        m_notificationSubject: action.config.title ?? '',
        m_notificationText: action.config.text,
        m_ringtoneName: action.config.ringtone ?? '',
        m_priority: priorityToInt(action.config.priority),
      };

    case 'toast':
      return {
        m_classType: 'ToastAction',
        m_messageText: action.config.text,
        m_imageResourceName: '',
        m_duration: action.config.duration === 'long' ? 1 : 0,
        m_position: positionToInt(action.config.position),
      };

    case 'speak_text':
      return {
        m_classType: 'SpeakTextAction',
        m_textToSay: action.config.text,
        m_queue: action.config.queue ?? false,
        m_speed: action.config.speed ?? 1.0,
        m_pitch: action.config.pitch ?? 1.0,
        m_waitToFinish: action.config.wait_to_finish ?? false,
      };

    case 'wifi':
      return {
        m_classType: 'SetWifiAction',
        m_ssid: action.config.ssid ?? '',
        m_networkId: -1,
        m_state: enableStateToInt(action.config.state),
      };

    case 'bluetooth':
      return {
        m_classType: 'SetBluetoothAction',
        m_deviceAddress: '',
        m_deviceName: action.config.device_name ?? '',
        m_state: enableStateToInt(action.config.state),
      };

    case 'volume':
      return {
        m_classType: 'SetVolumeAction',
        m_streamIndexArray: [streamToIndex(action.config.stream)],
        m_streamVolumeArray: [action.config.level],
        m_forceVibrateOff: false,
      };

    case 'launch_app':
      return {
        m_classType: 'LaunchActivityAction',
        m_option: 0,
        m_launchByPackageName: true,
        m_packageName: action.config.package_name,
      };

    case 'http_request':
      return {
        m_classType: 'OpenWebPageAction',
        m_urlToOpen: action.config.url,
        m_httpGet: action.config.method === 'GET',
        m_disableUrlEncode: false,
        m_blockNextAction: false,
      };

    case 'termux_script':
      // ⚠️ クラス名・フィールド名は T0-1 で要確認
      return {
        m_classType: 'RunScriptAction',
        m_scriptText: `bash ~/macrodroid/${action.config.script.replace('scripts/', '')} ${action.config.args ?? ''}`.trim(),
      };

    case 'set_variable':
      return {
        m_classType: 'SetVariableAction',
        m_variable: { m_name: action.config.name, m_type: 2 },
        m_newStringValue: action.config.value,
        m_userPrompt: action.config.prompt_user ?? false,
        m_newIntValue: 0,
      };

    case 'if_else':
      // ⚠️ if_else の内部構造は T0-1 で要確認
      return {
        m_classType: 'IfAction',
        m_constraintList: [conditionToMdr(action.config.condition)],
        m_ifActions: action.config.then_actions.map(actionToMdr),
        m_elseActions: (action.config.else_actions ?? []).map(actionToMdr),
      };

    case 'wait':
      return {
        m_classType: 'PauseAction',
        m_pauseDurationSeconds: action.config.duration_ms / 1000,
      };

    case 'disable_macro':
      return {
        m_classType: 'DisableMacroAction',
        m_macroName: action.config.macro_name,
        m_state: action.config.state === 'enable' ? 1 : 0,
        m_GUID: 0,
      };

    case 'force_macro_run':
      return {
        m_classType: 'ForceMacroRunAction',
        m_guid: 0,
        m_macroName: action.config.macro_name,
        m_ignoreConstraints: action.config.ignore_constraints ?? false,
      };

    case 'raw':
      return { m_classType: action.config.classType, ...action.config.rawData };
  }
}

// ─── 条件変換 ─────────────────────────────────────────────────────────────

function conditionToMdr(condition: ConditionDefinition): MdrConstraint {
  const negate = 'negate' in condition && condition.negate === true;

  switch (condition.type) {
    case 'time_range': {
      const [sh, sm] = condition.config.from.split(':').map(Number);
      const [eh, em] = condition.config.to.split(':').map(Number);
      return {
        m_classType: 'TimeOfDayConstraint',
        m_startHour: sh ?? 0,
        m_startMinute: sm ?? 0,
        m_endHour: eh ?? 0,
        m_endMinute: em ?? 0,
        m_isNot: negate,
      };
    }

    case 'day_of_week':
      return {
        m_classType: 'DayOfWeekConstraint',
        m_daysOfWeek: condition.config.days.map(dayToIndex),
        m_isNot: negate,
      };

    case 'battery_level':
      return {
        m_classType: 'BatteryLevelConstraint',
        m_batteryLevel: condition.config.value,
        m_equals: condition.config.operator === 'eq',
        m_greaterThan: ['gt', 'gte'].includes(condition.config.operator),
        m_isNot: negate,
      };

    case 'wifi_connected':
      return {
        m_classType: 'WifiConstraint',
        m_ssidList: condition.config?.ssid ? [condition.config.ssid] : [],
        m_wifiState: 1,
        m_isNot: negate,
      };

    case 'screen_on':
      return { m_classType: 'ScreenOnOffConstraint', m_screenOn: true, m_isNot: negate };

    case 'variable':
      return {
        m_classType: 'MacroDroidVariableConstraint',
        m_variable: { m_name: condition.config.name, m_type: 2 },
        m_intValue: 0,
        m_stringEqual: condition.config.value,
        m_booleanValue: false,
        m_intGreaterThan: ['gt', 'gte'].includes(condition.config.operator),
        m_intLessThan: ['lt', 'lte'].includes(condition.config.operator),
        m_intNotEqual: condition.config.operator === 'neq',
        m_stringComparisonType: condition.config.operator === 'eq' ? 0 : 1,
        m_enableRegex: false,
        m_isNot: negate,
      };

    case 'raw':
      return { m_classType: condition.config.classType, ...condition.config.rawData };
  }
}

// ─── ユーティリティ ───────────────────────────────────────────────────────

const DAY_ORDER: Record<string, number> = { sun: 0, mon: 1, tue: 2, wed: 3, thu: 4, fri: 5, sat: 6 };
function dayToIndex(day: string): number { return DAY_ORDER[day] ?? 0; }

function priorityToInt(p?: string): number {
  return p === 'high' ? 2 : p === 'low' ? 0 : 1;
}

function positionToInt(p?: string): number {
  return p === 'top' ? 0 : p === 'bottom' ? 2 : 1;
}

function enableStateToInt(s: string): number {
  return s === 'enable' ? 1 : s === 'disable' ? 0 : 2;
}

function streamToIndex(stream: string): number {
  const map: Record<string, number> = { ring: 2, media: 3, alarm: 4, notification: 5, system: 1 };
  return map[stream] ?? 3;
}

function extractCategories(scenario: Scenario) {
  const names = [...new Set(scenario.macros.map((m) => m.category).filter(Boolean))];
  return names.map((name) => ({ m_name: name! }));
}

function generateGuid(): number {
  return Math.floor(Math.random() * 2_000_000_000);
}
