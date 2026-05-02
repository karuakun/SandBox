// MacroDroid .mdr ファイルの内部型定義
// 参照: docs/03_mdr_format.md
// ⚠️ フィールド名は T0-1 実機検証で確定すること

// ─── トップレベル ──────────────────────────────────────────────────────────

export interface MdrFile {
  macroList: MdrMacro[];
  categoryList?: MdrCategory[];
  variableList?: MdrVariable[];
  cellTowerGroups?: unknown[];
  cellTowersIgnore?: unknown[];
  drawerConfiguration?: unknown;
  userIcons?: unknown[];
}

export interface MdrCategory {
  m_name: string;
}

export interface MdrVariable {
  m_name: string;
  m_type: number;
  m_stringValue?: string;
  m_intValue?: number;
  m_booleanValue?: boolean;
}

// ─── マクロ ───────────────────────────────────────────────────────────────

export interface MdrMacro {
  m_GUID: number;               // ⚠️ 整数か文字列か要確認
  m_name: string;
  m_description: string;
  m_isEnabled: boolean;         // ⚠️ m_enabled の可能性あり
  m_category: string;
  m_triggerList: MdrTrigger[];
  m_actionList: MdrAction[];
  m_constraintList: MdrConstraint[];
  m_isOrCondition: boolean;
  m_excludeLog?: boolean;
  m_headingColor?: number;
  localVariables?: MdrVariable[];
}

// ─── 共通ベース ───────────────────────────────────────────────────────────

interface MdrElement {
  m_classType: string;
  m_constraintList?: MdrConstraint[];
  m_isOrCondition?: boolean;
  m_isDisabled?: boolean;
  m_SIGUID?: number;
}

// ─── トリガー ─────────────────────────────────────────────────────────────

export type MdrTrigger = MdrElement & Record<string, unknown>;

export interface MdrTimerTrigger extends MdrElement {
  m_classType: 'TimerTrigger';
  m_daysOfWeek: number[];   // 0=Sun, 1=Mon, ..., 6=Sat
  m_hour: number;
  m_minute: number;
}

export interface MdrRegularIntervalTrigger extends MdrElement {
  m_classType: 'RegularIntervalTrigger';
  m_seconds: number;
  m_startHour: number;
}

export interface MdrWifiConnectionTrigger extends MdrElement {
  m_classType: 'WifiConnectionTrigger';
  m_ssidList: string[];
  m_wifiState: number;      // ⚠️ 0=切断, 1=接続 など要確認
}

export interface MdrScreenOnOffTrigger extends MdrElement {
  m_classType: 'ScreenOnOffTrigger';
  m_screenOn: boolean;
}

export interface MdrBatteryLevelTrigger extends MdrElement {
  m_classType: 'BatteryLevelTrigger';
  m_batteryLevel: number;
  m_decreasesTo: boolean;
}

export interface MdrWebHookTrigger extends MdrElement {
  m_classType: 'WebHookTrigger';
  m_identifier: string;
  m_url: string;
}

export interface MdrNotificationTrigger extends MdrElement {
  m_classType: 'NotificationTrigger';
  m_textContent: string;
  m_packageNameList: string[];
}

export interface MdrApplicationLaunchedTrigger extends MdrElement {
  m_classType: 'ApplicationLaunchedTrigger';
  m_applicationNameList: string[];
  m_packageNameList: string[];
  m_launched: boolean;
}

export interface MdrGeofenceTrigger extends MdrElement {
  m_classType: 'GeofenceTrigger';
  m_geofenceId: number;
  m_enterArea: boolean;
}

export interface MdrBootTrigger extends MdrElement {
  m_classType: 'BootTrigger';
}

export interface MdrDeviceUnlockedTrigger extends MdrElement {
  m_classType: 'DeviceUnlockedTrigger';
}

export interface MdrEmptyTrigger extends MdrElement {
  m_classType: 'EmptyTrigger';
}

// ─── アクション ───────────────────────────────────────────────────────────

export type MdrAction = MdrElement & Record<string, unknown>;

export interface MdrNotificationAction extends MdrElement {
  m_classType: 'NotificationAction';
  m_notificationSubject: string;  // ⚠️ フィールド名要確認
  m_notificationText: string;     // ⚠️ フィールド名要確認
  m_ringtoneName: string;
  m_priority: number;
}

export interface MdrToastAction extends MdrElement {
  m_classType: 'ToastAction';
  m_messageText: string;          // ⚠️ フィールド名要確認
  m_imageResourceName: string;
  m_duration: number;
  m_position: number;
}

export interface MdrSpeakTextAction extends MdrElement {
  m_classType: 'SpeakTextAction';
  m_textToSay: string;            // ⚠️ フィールド名要確認
  m_queue: boolean;
  m_speed: number;
  m_pitch: number;
  m_waitToFinish: boolean;
}

export interface MdrSetWifiAction extends MdrElement {
  m_classType: 'SetWifiAction';
  m_ssid: string;
  m_networkId: number;
  m_state: number;
}

export interface MdrSetBluetoothAction extends MdrElement {
  m_classType: 'SetBluetoothAction';
  m_deviceAddress: string;
  m_deviceName: string;
  m_state: number;
}

export interface MdrSetVolumeAction extends MdrElement {
  m_classType: 'SetVolumeAction';
  m_streamIndexArray: number[];
  m_streamVolumeArray: number[];
  m_forceVibrateOff: boolean;
}

export interface MdrLaunchActivityAction extends MdrElement {
  m_classType: 'LaunchActivityAction';
  m_option: number;
  m_launchByPackageName: boolean;
}

export interface MdrOpenWebPageAction extends MdrElement {
  m_classType: 'OpenWebPageAction';
  m_urlToOpen: string;            // ⚠️ フィールド名要確認
  m_httpGet: boolean;
  m_disableUrlEncode: boolean;
  m_blockNextAction: boolean;
}

export interface MdrSetVariableAction extends MdrElement {
  m_classType: 'SetVariableAction';
  m_variable: MdrVariable;
  m_newStringValue: string;
  m_userPrompt: boolean;
  m_newIntValue: number;
}

// ⚠️ Termux スクリプト実行アクション: クラス名・フィールドは T0-1 で要確認
export interface MdrRunScriptAction extends MdrElement {
  m_classType: string;            // 'RunScriptAction' or 別名
  m_scriptText?: string;          // ⚠️ 要確認
  m_path?: string;                // ⚠️ 要確認
}

export interface MdrDisableMacroAction extends MdrElement {
  m_classType: 'DisableMacroAction';
  m_macroName: string;
  m_state: number;
  m_GUID: number;
}

export interface MdrForceMacroRunAction extends MdrElement {
  m_classType: 'ForceMacroRunAction';
  m_guid: number;
  m_macroName: string;
  m_ignoreConstraints: boolean;
}

// ─── 条件（Constraint）───────────────────────────────────────────────────

export type MdrConstraint = MdrElement & Record<string, unknown>;

export interface MdrDayOfWeekConstraint extends MdrElement {
  m_classType: 'DayOfWeekConstraint';
  m_daysOfWeek: number[];
}

export interface MdrTimeOfDayConstraint extends MdrElement {
  m_classType: 'TimeOfDayConstraint';
  m_startHour: number;
  m_startMinute: number;
  m_endHour: number;
  m_endMinute: number;
}

export interface MdrBatteryLevelConstraint extends MdrElement {
  m_classType: 'BatteryLevelConstraint';
  m_batteryLevel: number;
  m_equals: boolean;
  m_greaterThan: boolean;
}

export interface MdrWifiConstraint extends MdrElement {
  m_classType: 'WifiConstraint';
  m_ssidList: string[];
  m_wifiState: number;
}

export interface MdrScreenOnOffConstraint extends MdrElement {
  m_classType: 'ScreenOnOffConstraint';
  m_screenOn: boolean;
}

export interface MdrMacroDroidVariableConstraint extends MdrElement {
  m_classType: 'MacroDroidVariableConstraint';
  m_variable: MdrVariable;
  m_intValue: number;
  m_stringEqual: string;
  m_booleanValue: boolean;
  m_intGreaterThan: boolean;
  m_intLessThan: boolean;
  m_intNotEqual: boolean;
  m_stringComparisonType: number;
  m_enableRegex: boolean;
}
