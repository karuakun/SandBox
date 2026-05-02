import { z } from 'zod';
import type { Scenario } from './types/schema.js';

// ─── 共通ユーティリティ ────────────────────────────────────────────────────

const dayOfWeek = z.enum(['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun']);
const compareOperator = z.enum(['eq', 'neq', 'gt', 'gte', 'lt', 'lte']);
const enableState = z.enum(['enable', 'disable', 'toggle']);
const hhMm = z.string().regex(/^\d{2}:\d{2}$/, 'HH:mm 形式で指定してください（例: 07:30）');

// ─── 条件スキーマ ─────────────────────────────────────────────────────────

const timeRangeConditionSchema = z.object({
  type: z.literal('time_range'),
  negate: z.boolean().optional(),
  config: z.object({ from: hhMm, to: hhMm }),
});

const dayOfWeekConditionSchema = z.object({
  type: z.literal('day_of_week'),
  negate: z.boolean().optional(),
  config: z.object({ days: z.array(dayOfWeek).min(1, '曜日を1つ以上指定してください') }),
});

const batteryLevelConditionSchema = z.object({
  type: z.literal('battery_level'),
  negate: z.boolean().optional(),
  config: z.object({ operator: compareOperator, value: z.number().int().min(0).max(100) }),
});

const wifiConnectedConditionSchema = z.object({
  type: z.literal('wifi_connected'),
  negate: z.boolean().optional(),
  config: z.object({ ssid: z.string().optional() }).optional(),
});

const screenOnConditionSchema = z.object({
  type: z.literal('screen_on'),
  negate: z.boolean().optional(),
  config: z.object({}).optional(),
});

const variableConditionSchema = z.object({
  type: z.literal('variable'),
  negate: z.boolean().optional(),
  config: z.object({ name: z.string().min(1), operator: compareOperator, value: z.string() }),
});

const rawConditionSchema = z.object({
  type: z.literal('raw'),
  negate: z.boolean().optional(),
  config: z.object({ classType: z.string(), rawData: z.record(z.unknown()) }),
});

const conditionSchema = z.discriminatedUnion('type', [
  timeRangeConditionSchema,
  dayOfWeekConditionSchema,
  batteryLevelConditionSchema,
  wifiConnectedConditionSchema,
  screenOnConditionSchema,
  variableConditionSchema,
  rawConditionSchema,
]);

// ─── アクションスキーマ ───────────────────────────────────────────────────

const notificationActionSchema = z.object({
  type: z.literal('notification'),
  config: z.object({
    title: z.string().optional(),
    text: z.string().min(1, '通知テキストは必須です'),
    priority: z.enum(['low', 'normal', 'high']).optional(),
    ringtone: z.string().optional(),
  }),
});

const toastActionSchema = z.object({
  type: z.literal('toast'),
  config: z.object({
    text: z.string().min(1),
    duration: z.enum(['short', 'long']).optional(),
    position: z.enum(['top', 'middle', 'bottom']).optional(),
  }),
});

const speakTextActionSchema = z.object({
  type: z.literal('speak_text'),
  config: z.object({
    text: z.string().min(1),
    queue: z.boolean().optional(),
    speed: z.number().min(0.1).max(4.0).optional(),
    pitch: z.number().min(0.1).max(4.0).optional(),
    wait_to_finish: z.boolean().optional(),
  }),
});

const wifiActionSchema = z.object({
  type: z.literal('wifi'),
  config: z.object({ state: enableState, ssid: z.string().optional() }),
});

const bluetoothActionSchema = z.object({
  type: z.literal('bluetooth'),
  config: z.object({ state: enableState, device_name: z.string().optional() }),
});

const volumeActionSchema = z.object({
  type: z.literal('volume'),
  config: z.object({
    stream: z.enum(['ring', 'media', 'alarm', 'notification', 'system']),
    level: z.number().int().min(0).max(100),
  }),
});

const launchAppActionSchema = z.object({
  type: z.literal('launch_app'),
  config: z.object({ package_name: z.string().min(1) }),
});

const httpRequestActionSchema = z.object({
  type: z.literal('http_request'),
  config: z.object({
    method: z.enum(['GET', 'POST', 'PUT', 'DELETE', 'PATCH']),
    url: z.string().url('有効な URL を指定してください'),
    body: z.string().optional(),
    headers: z.record(z.string()).optional(),
  }),
});

const termuxScriptActionSchema = z.object({
  type: z.literal('termux_script'),
  config: z.object({
    script: z.string().min(1, 'スクリプトパスは必須です').regex(/^scripts\//, 'scripts/ から始まるパスを指定してください'),
    args: z.string().optional(),
    wait_for_result: z.boolean().optional(),
    timeout_sec: z.number().int().positive().optional(),
  }),
});

const setVariableActionSchema = z.object({
  type: z.literal('set_variable'),
  config: z.object({
    name: z.string().min(1),
    value: z.string(),
    prompt_user: z.boolean().optional(),
  }),
});

const waitActionSchema = z.object({
  type: z.literal('wait'),
  config: z.object({ duration_ms: z.number().int().positive() }),
});

const disableMacroActionSchema = z.object({
  type: z.literal('disable_macro'),
  config: z.object({ macro_name: z.string().min(1), state: z.enum(['enable', 'disable']) }),
});

const forceMacroRunActionSchema = z.object({
  type: z.literal('force_macro_run'),
  config: z.object({ macro_name: z.string().min(1), ignore_constraints: z.boolean().optional() }),
});

const rawActionSchema = z.object({
  type: z.literal('raw'),
  config: z.object({ classType: z.string(), rawData: z.record(z.unknown()) }),
});

// if_else は再帰参照のため lazy で定義
const actionSchemaBase = z.discriminatedUnion('type', [
  notificationActionSchema,
  toastActionSchema,
  speakTextActionSchema,
  wifiActionSchema,
  bluetoothActionSchema,
  volumeActionSchema,
  launchAppActionSchema,
  httpRequestActionSchema,
  termuxScriptActionSchema,
  setVariableActionSchema,
  waitActionSchema,
  disableMacroActionSchema,
  forceMacroRunActionSchema,
  rawActionSchema,
]);

type ActionSchemaBase = z.infer<typeof actionSchemaBase>;

interface IfElseActionSchema {
  type: 'if_else';
  config: {
    condition: z.infer<typeof conditionSchema>;
    then_actions: ActionSchemaBase[];
    else_actions?: ActionSchemaBase[];
  };
}

const ifElseActionSchema: z.ZodType<IfElseActionSchema> = z.lazy(() =>
  z.object({
    type: z.literal('if_else'),
    config: z.object({
      condition: conditionSchema,
      then_actions: z.array(actionSchema).min(1),
      else_actions: z.array(actionSchema).optional(),
    }),
  })
);

const actionSchema: z.ZodType<ActionSchemaBase | IfElseActionSchema> = z.union([
  actionSchemaBase,
  ifElseActionSchema,
]);

// ─── トリガースキーマ ─────────────────────────────────────────────────────

const triggerSchema = z.discriminatedUnion('type', [
  z.object({ type: z.literal('time'), config: z.object({ time: hhMm, days: z.array(dayOfWeek).optional() }) }),
  z.object({ type: z.literal('interval'), config: z.object({ interval_minutes: z.number().int().positive(), start_hour: z.number().int().min(0).max(23).optional() }) }),
  z.object({ type: z.literal('wifi_connected'), config: z.object({ ssid: z.string().optional() }).optional() }),
  z.object({ type: z.literal('wifi_disconnected'), config: z.object({ ssid: z.string().optional() }).optional() }),
  z.object({ type: z.literal('screen_on'), config: z.object({}).optional() }),
  z.object({ type: z.literal('screen_off'), config: z.object({}).optional() }),
  z.object({ type: z.literal('battery_level'), config: z.object({ level: z.number().int().min(0).max(100), direction: z.enum(['above', 'below']) }) }),
  z.object({ type: z.literal('webhook'), config: z.object({ identifier: z.string().min(1) }) }),
  z.object({ type: z.literal('notification_received'), config: z.object({ app_package: z.string().optional(), text_content: z.string().optional(), enable_regex: z.boolean().optional() }).optional() }),
  z.object({ type: z.literal('app_launched'), config: z.object({ package_name: z.string().min(1) }) }),
  z.object({ type: z.literal('location_enter'), config: z.object({ lat: z.number(), lng: z.number(), radius_m: z.number().positive(), name: z.string().optional() }) }),
  z.object({ type: z.literal('location_exit'), config: z.object({ lat: z.number(), lng: z.number(), radius_m: z.number().positive(), name: z.string().optional() }) }),
  z.object({ type: z.literal('boot'), config: z.object({}).optional() }),
  z.object({ type: z.literal('device_unlocked'), config: z.object({}).optional() }),
  z.object({ type: z.literal('manual'), config: z.object({}).optional() }),
  z.object({ type: z.literal('raw'), config: z.object({ classType: z.string(), rawData: z.record(z.unknown()) }) }),
]);

// ─── マクロ・シナリオスキーマ ─────────────────────────────────────────────

const macroSchema = z.object({
  id: z.string().min(1).regex(/^[a-z0-9-]+$/, 'id は小文字英数字とハイフンのみ使用できます'),
  name: z.string().min(1),
  enabled: z.boolean(),
  category: z.string().optional(),
  trigger: triggerSchema,
  conditions: z.array(conditionSchema).optional(),
  actions: z.array(actionSchema).min(1, 'アクションを1つ以上指定してください'),
});

export const scenarioSchema = z.object({
  apiVersion: z.literal('macrodroid/v1'),
  kind: z.literal('Scenario'),
  metadata: z.object({
    id: z.string().min(1).regex(/^[a-z0-9-]+$/, 'id は小文字英数字とハイフンのみ使用できます'),
    name: z.string().min(1),
    description: z.string().optional(),
    tags: z.array(z.string()).optional(),
    version: z.number().int().positive(),
    createdAt: z.string(),
    updatedAt: z.string(),
  }),
  macros: z.array(macroSchema).min(1, 'マクロを1つ以上定義してください'),
});

// ─── バリデーション関数 ───────────────────────────────────────────────────

export interface ValidationResult {
  success: boolean;
  data?: Scenario;
  errors?: ValidationError[];
}

export interface ValidationError {
  path: string;
  message: string;
}

export function validateScenario(input: unknown): ValidationResult {
  const result = scenarioSchema.safeParse(input);
  if (result.success) {
    return { success: true, data: result.data as Scenario };
  }
  const errors: ValidationError[] = result.error.errors.map((e) => ({
    path: e.path.join('.'),
    message: e.message,
  }));
  return { success: false, errors };
}
