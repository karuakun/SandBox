// YAML シナリオスキーマの TypeScript 型定義
// 対応仕様: docs/01_spec.md Section 4

export type ApiVersion = 'macrodroid/v1';
export type Kind = 'Scenario';
export type DayOfWeek = 'mon' | 'tue' | 'wed' | 'thu' | 'fri' | 'sat' | 'sun';
export type CompareOperator = 'eq' | 'neq' | 'gt' | 'gte' | 'lt' | 'lte';
export type EnableState = 'enable' | 'disable' | 'toggle';

// ─── Scenario ─────────────────────────────────────────────────────────────

export interface Scenario {
  apiVersion: ApiVersion;
  kind: Kind;
  metadata: ScenarioMetadata;
  macros: MacroDefinition[];
}

export interface ScenarioMetadata {
  id: string;
  name: string;
  description?: string;
  tags?: string[];
  version: number;
  createdAt: string;
  updatedAt: string;
}

// ─── Macro ────────────────────────────────────────────────────────────────

export interface MacroDefinition {
  id: string;
  name: string;
  enabled: boolean;
  category?: string;
  trigger: TriggerDefinition;
  conditions?: ConditionDefinition[];
  actions: ActionDefinition[];
}

// ─── Triggers ─────────────────────────────────────────────────────────────

export type TriggerDefinition =
  | TimeTrigger
  | IntervalTrigger
  | WifiConnectedTrigger
  | WifiDisconnectedTrigger
  | ScreenOnTrigger
  | ScreenOffTrigger
  | BatteryLevelTrigger
  | WebhookTrigger
  | NotificationReceivedTrigger
  | AppLaunchedTrigger
  | LocationEnterTrigger
  | LocationExitTrigger
  | BootTrigger
  | DeviceUnlockedTrigger
  | ManualTrigger
  | RawTrigger;

interface BaseTrigger<T extends string> {
  type: T;
}

export interface TimeTrigger extends BaseTrigger<'time'> {
  config: {
    time: string; // HH:mm
    days?: DayOfWeek[];
  };
}

export interface IntervalTrigger extends BaseTrigger<'interval'> {
  config: {
    interval_minutes: number;
    start_hour?: number;
  };
}

export interface WifiConnectedTrigger extends BaseTrigger<'wifi_connected'> {
  config: {
    ssid?: string;
  };
}

export interface WifiDisconnectedTrigger extends BaseTrigger<'wifi_disconnected'> {
  config: {
    ssid?: string;
  };
}

export interface ScreenOnTrigger extends BaseTrigger<'screen_on'> {
  config?: Record<string, never>;
}

export interface ScreenOffTrigger extends BaseTrigger<'screen_off'> {
  config?: Record<string, never>;
}

export interface BatteryLevelTrigger extends BaseTrigger<'battery_level'> {
  config: {
    level: number;
    direction: 'above' | 'below';
  };
}

export interface WebhookTrigger extends BaseTrigger<'webhook'> {
  config: {
    identifier: string;
  };
}

export interface NotificationReceivedTrigger extends BaseTrigger<'notification_received'> {
  config: {
    app_package?: string;
    text_content?: string;
    enable_regex?: boolean;
  };
}

export interface AppLaunchedTrigger extends BaseTrigger<'app_launched'> {
  config: {
    package_name: string;
  };
}

export interface LocationEnterTrigger extends BaseTrigger<'location_enter'> {
  config: {
    lat: number;
    lng: number;
    radius_m: number;
    name?: string;
  };
}

export interface LocationExitTrigger extends BaseTrigger<'location_exit'> {
  config: {
    lat: number;
    lng: number;
    radius_m: number;
    name?: string;
  };
}

export interface BootTrigger extends BaseTrigger<'boot'> {
  config?: Record<string, never>;
}

export interface DeviceUnlockedTrigger extends BaseTrigger<'device_unlocked'> {
  config?: Record<string, never>;
}

export interface ManualTrigger extends BaseTrigger<'manual'> {
  config?: Record<string, never>;
}

export interface RawTrigger extends BaseTrigger<'raw'> {
  config: {
    classType: string;
    rawData: Record<string, unknown>;
  };
}

// ─── Actions ──────────────────────────────────────────────────────────────

export type ActionDefinition =
  | NotificationAction
  | ToastAction
  | SpeakTextAction
  | WifiAction
  | BluetoothAction
  | VolumeAction
  | LaunchAppAction
  | HttpRequestAction
  | TermuxScriptAction
  | SetVariableAction
  | IfElseAction
  | WaitAction
  | DisableMacroAction
  | ForceMacroRunAction
  | RawAction;

interface BaseAction<T extends string> {
  type: T;
}

export interface NotificationAction extends BaseAction<'notification'> {
  config: {
    title?: string;
    text: string;
    priority?: 'low' | 'normal' | 'high';
    ringtone?: string;
  };
}

export interface ToastAction extends BaseAction<'toast'> {
  config: {
    text: string;
    duration?: 'short' | 'long';
    position?: 'top' | 'middle' | 'bottom';
  };
}

export interface SpeakTextAction extends BaseAction<'speak_text'> {
  config: {
    text: string;
    queue?: boolean;
    speed?: number;
    pitch?: number;
    wait_to_finish?: boolean;
  };
}

export interface WifiAction extends BaseAction<'wifi'> {
  config: {
    state: EnableState;
    ssid?: string;
  };
}

export interface BluetoothAction extends BaseAction<'bluetooth'> {
  config: {
    state: EnableState;
    device_name?: string;
  };
}

export interface VolumeAction extends BaseAction<'volume'> {
  config: {
    stream: 'ring' | 'media' | 'alarm' | 'notification' | 'system';
    level: number;
  };
}

export interface LaunchAppAction extends BaseAction<'launch_app'> {
  config: {
    package_name: string;
  };
}

export interface HttpRequestAction extends BaseAction<'http_request'> {
  config: {
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
    url: string;
    body?: string;
    headers?: Record<string, string>;
  };
}

export interface TermuxScriptAction extends BaseAction<'termux_script'> {
  config: {
    script: string;          // scripts/ 以下の相対パス
    args?: string;
    wait_for_result?: boolean;
    timeout_sec?: number;
  };
}

export interface SetVariableAction extends BaseAction<'set_variable'> {
  config: {
    name: string;
    value: string;
    prompt_user?: boolean;
  };
}

export interface IfElseAction extends BaseAction<'if_else'> {
  config: {
    condition: ConditionDefinition;
    then_actions: ActionDefinition[];
    else_actions?: ActionDefinition[];
  };
}

export interface WaitAction extends BaseAction<'wait'> {
  config: {
    duration_ms: number;
  };
}

export interface DisableMacroAction extends BaseAction<'disable_macro'> {
  config: {
    macro_name: string;
    state: 'enable' | 'disable';
  };
}

export interface ForceMacroRunAction extends BaseAction<'force_macro_run'> {
  config: {
    macro_name: string;
    ignore_constraints?: boolean;
  };
}

export interface RawAction extends BaseAction<'raw'> {
  config: {
    classType: string;
    rawData: Record<string, unknown>;
  };
}

// ─── Conditions ───────────────────────────────────────────────────────────

export type ConditionDefinition =
  | TimeRangeCondition
  | DayOfWeekCondition
  | BatteryLevelCondition
  | WifiConnectedCondition
  | ScreenOnCondition
  | VariableCondition
  | RawCondition;

interface BaseCondition<T extends string> {
  type: T;
  negate?: boolean;
}

export interface TimeRangeCondition extends BaseCondition<'time_range'> {
  config: {
    from: string; // HH:mm
    to: string;   // HH:mm
  };
}

export interface DayOfWeekCondition extends BaseCondition<'day_of_week'> {
  config: {
    days: DayOfWeek[];
  };
}

export interface BatteryLevelCondition extends BaseCondition<'battery_level'> {
  config: {
    operator: CompareOperator;
    value: number;
  };
}

export interface WifiConnectedCondition extends BaseCondition<'wifi_connected'> {
  config: {
    ssid?: string;
  };
}

export interface ScreenOnCondition extends BaseCondition<'screen_on'> {
  config?: Record<string, never>;
}

export interface VariableCondition extends BaseCondition<'variable'> {
  config: {
    name: string;
    operator: CompareOperator;
    value: string;
  };
}

export interface RawCondition extends BaseCondition<'raw'> {
  config: {
    classType: string;
    rawData: Record<string, unknown>;
  };
}

// ─── Type guards ──────────────────────────────────────────────────────────

export function isScenario(value: unknown): value is Scenario {
  return (
    typeof value === 'object' &&
    value !== null &&
    (value as Scenario).apiVersion === 'macrodroid/v1' &&
    (value as Scenario).kind === 'Scenario'
  );
}
