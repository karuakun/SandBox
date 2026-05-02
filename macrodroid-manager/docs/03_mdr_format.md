# MacroDroid .mdr フォーマット解析

**作成日**: 2026-05-02  
**調査源**: ruby-macrodroid gem ソース解析 + コミュニティフォーラム  
**ステータス**: オンライン調査済み（実機検証は T0-1 で実施）

---

## 1. ファイル形式

| 項目 | 内容 |
|------|------|
| 拡張子 | `.mdr` |
| 形式 | JSON テキスト（コミュニティ報告。実機検証で ZIP の可能性も確認すること） |
| 文字コード | UTF-8 |

> **T0-1 で確認すること**: `file` コマンドや unzip コマンドで ZIP か plain JSON かを確認する。

---

## 2. トップレベル構造

```json
{
  "macroList": [ /* マクロ定義の配列 */ ],
  "categoryList": [ /* カテゴリ一覧 */ ],
  "variableList": [ /* グローバル変数一覧 */ ],
  "cellTowerGroups": [],
  "cellTowersIgnore": [],
  "drawerConfiguration": { /* 通知ドロワー設定 */ },
  "userIcons": []
}
```

---

## 3. マクロオブジェクト

```json
{
  "m_GUID": 1234567890,
  "m_name": "マクロ名",
  "m_description": "説明",
  "m_isEnabled": true,
  "m_category": "カテゴリ名",
  "m_triggerList": [ /* トリガー配列 */ ],
  "m_actionList":  [ /* アクション配列 */ ],
  "m_constraintList": [ /* 制約（条件）配列 */ ],
  "m_isOrCondition": false,
  "m_excludeLog": false,
  "m_headingColor": 0,
  "localVariables": []
}
```

> **注意**: `m_GUID` は UUID 文字列ではなく整数値の可能性がある（要実機確認）。

---

## 4. 共通フィールド（全 trigger / action / constraint）

各要素は最低限以下のフィールドを持つ：

```json
{
  "m_classType": "クラス名",
  "m_constraintList": [],
  "m_isOrCondition": false,
  "m_isDisabled": false,
  "m_SIGUID": 123456789
}
```

---

## 5. トリガークラス一覧

| m_classType | 説明 | 主要フィールド |
|-------------|------|----------------|
| `TimerTrigger` | 指定時刻 | `m_daysOfWeek`, `m_hour`, `m_minute` |
| `RegularIntervalTrigger` | 定期実行 | `m_seconds`, `m_startHour` |
| `WifiConnectionTrigger` | Wi-Fi 接続/切断 | `m_ssidList`, `m_wifiState` |
| `WifiConnectionTrigger2` | Wi-Fi 接続/切断 v2 | `m_ssidList`, `m_wifiState` |
| `WifiSSIDTrigger` | Wi-Fi SSID 範囲内外 | `m_wifiCellInfoList`, `m_inRange` |
| `WebHookTrigger` | Webhook 受信 | `m_identifier`, `m_url` |
| `ScreenOnOffTrigger` | 画面点灯/消灯 | `m_screenOn` |
| `DeviceUnlockedTrigger` | デバイスロック解除 | （なし） |
| `BootTrigger` | 端末起動 | （なし） |
| `BatteryLevelTrigger` | バッテリーレベル到達 | `m_batteryLevel`, `m_decreasesTo` |
| `ExternalPowerTrigger` | 電源接続/切断 | `m_powerConnected`, `m_powerConnectedOptions` |
| `GeofenceTrigger` | ジオフェンス進入/退出 | `m_geofenceId`, `m_enterArea` |
| `NotificationTrigger` | 通知受信 | `m_textContent`, `m_packageNameList` |
| `ApplicationLaunchedTrigger` | アプリ起動 | `m_applicationNameList`, `m_packageNameList`, `m_launched` |
| `ApplicationInstalledRemovedTrigger` | アプリインストール/削除 | `m_applicationNameList`, `m_packageNameList`, `m_installed`, `m_updated` |
| `IncomingCallTrigger` | 着信 | `m_incomingCallFromList`, `m_phoneNumberExclude` |
| `OutgoingCallTrigger` | 発信 | `m_outgoingCallToList` |
| `CallActiveTrigger` | 通話中 | `m_contactList`, `m_signalOn` |
| `CallEndedTrigger` | 通話終了 | `m_contactList` |
| `CallMissedTrigger` | 不在着信 | `m_contactList` |
| `IncomingSMSTrigger` | SMS 受信 | `m_smsFromList`, `m_smsContent`, `m_enableRegex` |
| `BluetoothTrigger` | Bluetooth 接続/切断 | `m_deviceName`, `m_btState` |
| `HeadphonesTrigger` | イヤホン接続/切断 | `m_headphonesConnected` |
| `NFCTrigger` | NFC タグ読取 | `m_tagName` |
| `CalendarTrigger` | カレンダーイベント | `m_titleText`, `m_calendarName`, `m_eventStart` |
| `DayTrigger` | 曜日/日付 | `m_dayOfWeek`, `m_dayOfMonth`, `m_monthOfYear` |
| `ShakeDeviceTrigger` | 端末シェイク | （なし） |
| `FlipDeviceTrigger` | 端末フリップ | `m_faceDown`, `m_anyStart` |
| `OrientationTrigger` | 向き変更 | `m_option`, `m_checkOrientationAlive` |
| `ActivityRecognitionTrigger` | 活動認識 | `m_confidenceLevel`, `m_selectedIndex` |
| `ProximityTrigger` | 近接センサー | `m_near`, `m_selectedOption` |
| `VolumeButtonTrigger` | 音量ボタン | `m_option`, `m_dontChangeVolume` |
| `FloatingButtonTrigger` | フローティングボタン | `m_imageResourceId`, `m_iconBgColor` |
| `ShortcutTrigger` | ショートカット | （なし） |
| `IntentReceivedTrigger` | Intent 受信 | `m_action`, `m_extraParams` |
| `GPSEnabledTrigger` | GPS 有効/無効 | `m_gpsModeEnabled` |
| `AirplaneModeTrigger` | 機内モード変更 | `m_airplaneModeEnabled` |
| `SilentModeTrigger` | サイレントモード変更 | `m_silentEnabled` |
| `EmptyTrigger` | 手動実行のみ | （なし） |
| `SignalOnOffTrigger` | 電波 ON/OFF | `m_signalOn` |
| `SunriseSunsetTrigger` | 日の出/日の入り | `m_option`, `m_timeAdjustSeconds` |
| `WeatherTrigger` | 天気条件 | `m_temperature`, `m_weatherCondition` |
| `UsbDeviceConnectionTrigger` | USB 接続 | `m_option` |
| `StopwatchTrigger` | ストップウォッチ | `m_stopwatchName`, `m_seconds` |
| `ClipboardChangeTrigger` | クリップボード変更 | `m_text`, `m_enableRegex` |
| `SwipeTrigger` | スワイプ | `m_swipeStartArea`, `m_swipeMotion` |
| `WidgetPressedTrigger` | ウィジェット押下 | `m_widgetType`, `m_widgetLabel` |
| `MediaButtonPressedTrigger` | メディアボタン | `m_option`, `m_cancelPress` |
| `FailedLoginTrigger` | ログイン失敗 | `m_numFailures` |
| `PowerButtonToggleTrigger` | 電源ボタン連打 | `m_numToggles` |

---

## 6. アクションクラス一覧

> **注意**: フィールド名は Ruby ライブラリの属性名（スネークケース）から推定。  
> 実際の JSON フィールドは `m_` プレフィックス + キャメルケースの可能性が高い（例: `notification_text` → `m_notificationText`）。  
> **T0-1 で実機エクスポートして確認すること。**

| m_classType | 説明 | 主要フィールド（推定） |
|-------------|------|----------------------|
| `NotificationAction` | 通知表示 | `m_notificationSubject`, `m_notificationText`, `m_ringtoneName`, `m_priority` |
| `ToastAction` | トースト表示 | `m_messageText`, `m_imageResourceName`, `m_duration`, `m_position` |
| `SpeakTextAction` | テキスト読み上げ | `m_textToSay`, `m_queue`, `m_speed`, `m_pitch`, `m_waitToFinish` |
| `MessageDialogAction` | メッセージダイアログ | `m_notificationText`, `m_notificationSubject` |
| `SetVariableAction` | 変数設定 | `m_variable`, `m_newStringValue`, `m_userPrompt`, `m_newIntValue` |
| `SetWifiAction` | Wi-Fi 制御 | `m_ssid`, `m_networkId`, `m_state` |
| `SetBluetoothAction` | Bluetooth 制御 | `m_deviceAddress`, `m_deviceName`, `m_state` |
| `SetHotspotAction` | ホットスポット制御 | `m_state`, `m_turnWifiOn`, `m_useLegacyMechanism` |
| `SetAirplaneModeAction` | 機内モード制御 | `m_state` |
| `SetVolumeAction` | 音量設定 | `m_streamIndexArray`, `m_streamVolumeArray`, `m_forceVibrateOff` |
| `SetBrightnessAction` | 輝度設定 | `m_brightnessPercent`, `m_forcePieMode` |
| `SetAutoRotateAction` | 自動回転 | `m_state` |
| `ScreenOnAction` | 画面制御 | `m_pieLockScreen`, `m_screenOff`, `m_screenOffNoLock` |
| `KeepAwakeAction` | スリープ抑制 | `m_enabled`, `m_permanent`, `m_screenOption`, `m_secondsToStayAwakeFor` |
| `VibrateAction` | バイブレーション | `m_vibratePattern` |
| `CameraFlashLightAction` | フラッシュライト | `m_launchForeground`, `m_state` |
| `LaunchActivityAction` | アプリ起動 | `m_option`, `m_launchByPackageName` |
| `OpenWebPageAction` | URL を開く | `m_urlToOpen`, `m_httpGet`, `m_disableUrlEncode`, `m_blockNextAction` |
| `SendIntentAction` | Intent 送信 | `m_action`, `m_className`, `m_data`, `m_packageName`, `m_target` |
| `UIInteractionAction` | UI 操作 | `m_uiInteractionConfiguration`, `m_action` |
| `ClipboardAction` | クリップボード操作 | `m_clipboardText` |
| `TakePictureAction` | 写真撮影 | `m_useFrontCamera`, `m_flashOption`, `m_path` |
| `TakeScreenshotAction` | スクリーンショット | `m_option`, `m_mechanism_option` |
| `SetAlarmClockAction` | アラーム設定 | `m_daysOfWeek`, `m_label`, `m_hour`, `m_minute`, `m_oneOff`, `m_option` |
| `SayTimeAction` | 時刻読み上げ | `m_12hour` |
| `StopWatchAction` | ストップウォッチ | `m_stopwatchName`, `m_option` |
| `DisableMacroAction` | マクロ有効/無効 | `m_macroName`, `m_state`, `m_GUID` |
| `ForceMacroRunAction` | マクロ強制実行 | `m_guid`, `m_macroName`, `m_ignoreConstraints` |
| `UDPCommandAction` | UDP コマンド送信 | `m_destination`, `m_port` |
| `ShareLocationAction` | 位置情報共有 | （要確認） |
| `RunScriptAction` ※要確認 | シェルスクリプト実行（Termux） | `m_scriptText` or `m_path`（要実機確認） |

> ※ `RunScriptAction` のクラス名・フィールドは ruby-macrodroid では見つからず。  
> **T0-1 で Termux スクリプト実行アクションを持つマクロをエクスポートして確認すること。**

---

## 7. 制約（条件）クラス一覧

| m_classType | 説明 | 主要フィールド |
|-------------|------|----------------|
| `DayOfWeekConstraint` | 曜日 | `m_daysOfWeek` |
| `TimeOfDayConstraint` | 時間帯 | `m_startHour`, `m_startMinute`, `m_endHour`, `m_endMinute` |
| `DayOfMonthConstraint` | 月の日付 | `m_daysOfMonth` |
| `MonthOfYearConstraint` | 月 | `m_months` |
| `BatteryLevelConstraint` | バッテリー残量 | `m_batteryLevel`, `m_equals`, `m_greaterThan` |
| `ExternalPowerConstraint` | 電源接続状態 | `m_externalPower`, `m_powerConnectedOptions` |
| `WifiConstraint` | Wi-Fi 状態 | `m_ssidList`, `m_wifiState` |
| `BluetoothConstraint` | Bluetooth 状態 | `m_anyDevice`, `m_btState`, `m_deviceName` |
| `ScreenOnOffConstraint` | 画面点灯状態 | `m_screenOn` |
| `DeviceLockedConstraint` | ロック状態 | `m_locked` |
| `MacroDroidVariableConstraint` | 変数比較 | `m_variable`, `m_intValue`, `m_stringEqual`, `m_booleanValue`, `m_stringComparisonType` |
| `MacroEnabledConstraint` | マクロ有効状態 | `m_enabled`, `m_macroIds`, `m_macroNames` |
| `ActiveApplicationConstraint` | フォアグラウンドアプリ | `m_applicationNameList`, `m_foreground`, `m_packageNameList` |
| `GPSEnabledConstraint` | GPS 状態 | `m_enabled` |
| `AirplaneModeConstraint` | 機内モード状態 | `m_enabled` |
| `SilentModeTrigger` | サイレントモード状態 | `m_silentEnabled` |
| `InCallConstraint` | 通話中 | `m_inCall` |
| `HeadphonesConnectionConstraint` | イヤホン接続状態 | `m_connected` |
| `NotificationPresentConstraint` | 通知存在確認 | `m_applicationNameList`, `m_textContent`, `m_exactMatch`, `m_enableRegex` |
| `CalendarConstraint` | カレンダー状態 | `m_calendarId`, `m_calendarName`, `m_titleText`, `m_availability` |
| `CellTowerConstraint` | セルタワー | `m_cellGroupName`, `m_cellIds`, `m_inRange` |
| `BrightnessConstraint` | 輝度 | `m_brightness`, `m_equals`, `m_greaterThan`, `m_isAutoBrightness` |
| `VolumeLevelConstraint` | 音量レベル | `m_comparison`, `m_streamIndexArray`, `m_volume` |
| `ProximitySensorConstraint` | 近接センサー | `m_near` |
| `LightLevelConstraint` | 照度 | `m_lightLevel`, `m_lightLevelFloat`, `m_option` |
| `FaceUpDownConstraint` | 端末向き（上面/下面） | `m_option`, `m_selectedOptions` |
| `DeviceOrientationConstraint` | 端末の向き | `m_portrait` |
| `MusicActiveConstraint` | 音楽再生中 | `m_musicActive` |
| `TriggerThatInvokedConstraint` | トリガー識別 | `m_not`, `m_siGuidThatInvoked`, `m_triggerName` |
| `LastRunTimeConstraint` | 最終実行時刻 | `m_checkThisMacro`, `m_invoked`, `m_timePeriodSeconds` |
| `TimeSinceBootConstraint` | 起動後経過時間 | `m_lessThan`, `m_timePeriodSeconds` |
| `VpnConstraint` | VPN 状態 | `m_option` |
| `IsRoamingConstraint` | ローミング状態 | `m_isRoaming` |
| `SignalOnOffConstraint` | 電波 ON/OFF | `m_option` |
| `DataOnOffConstraint` | データ通信 | `m_dataOn` |
| `IsRootedConstraint` | root 状態 | `m_rooted` |

---

## 8. YAML type → m_classType マッピング表

YAML スキーマの `type` と MacroDroid 内部クラスの対応。

### トリガー

| YAML type | m_classType | 確認状態 |
|-----------|-------------|---------|
| `time` | `TimerTrigger` | ✅ 確認済 |
| `interval` | `RegularIntervalTrigger` | ✅ 確認済 |
| `wifi_connected` / `wifi_disconnected` | `WifiConnectionTrigger` | ✅ 確認済 |
| `screen_on` / `screen_off` | `ScreenOnOffTrigger` | ✅ 確認済 |
| `battery_level` | `BatteryLevelTrigger` | ✅ 確認済 |
| `webhook` | `WebHookTrigger` | ✅ 確認済 |
| `notification_received` | `NotificationTrigger` | ✅ 確認済 |
| `app_launched` | `ApplicationLaunchedTrigger` | ✅ 確認済 |
| `location_enter` | `GeofenceTrigger` (m_enterArea: true) | ✅ 確認済 |
| `location_exit` | `GeofenceTrigger` (m_enterArea: false) | ✅ 確認済 |
| `boot` | `BootTrigger` | ✅ 確認済 |
| `device_unlocked` | `DeviceUnlockedTrigger` | ✅ 確認済 |
| `file_modified` | 要確認 | ⚠️ T0-1 で検証 |

### アクション

| YAML type | m_classType | 確認状態 |
|-----------|-------------|---------|
| `notification` | `NotificationAction` | ✅ 確認済 |
| `toast` | `ToastAction` | ✅ 確認済 |
| `wifi` | `SetWifiAction` | ✅ 確認済 |
| `bluetooth` | `SetBluetoothAction` | ✅ 確認済 |
| `volume` | `SetVolumeAction` | ✅ 確認済 |
| `launch_app` | `LaunchActivityAction` | ✅ 確認済 |
| `http_request` | `OpenWebPageAction` or `SendIntentAction` | ⚠️ T0-1 で検証 |
| `termux_script` | `RunScriptAction`（クラス名要確認） | ⚠️ T0-1 で検証 |
| `set_variable` | `SetVariableAction` | ✅ 確認済 |
| `speak_text` | `SpeakTextAction` | ✅ 確認済 |
| `wait` | 要確認 | ⚠️ T0-1 で検証 |
| `if_else` | 要確認（If/Else 構造） | ⚠️ T0-1 で検証 |

### 条件（Constraint）

| YAML type | m_classType | 確認状態 |
|-----------|-------------|---------|
| `time_range` | `TimeOfDayConstraint` | ✅ 確認済 |
| `day_of_week` | `DayOfWeekConstraint` | ✅ 確認済 |
| `battery_level` | `BatteryLevelConstraint` | ✅ 確認済 |
| `wifi_connected` | `WifiConstraint` | ✅ 確認済 |
| `screen_on` | `ScreenOnOffConstraint` | ✅ 確認済 |
| `variable` | `MacroDroidVariableConstraint` | ✅ 確認済 |

---

## 9. T0-1 実機検証チェックリスト

実機で以下のマクロを作成してエクスポートし、JSON 構造を確認する。

- [ ] 最小構成（時刻トリガー + トースト）→ ファイル形式（ZIP か JSON か）を確認
- [ ] Wi-Fi アクション → `SetWifiAction` のフィールド名確認
- [ ] HTTP リクエストアクション → クラス名・フィールド確認
- [ ] **Termux スクリプト実行アクション → クラス名・フィールド確認（最重要）**
- [ ] If/Else 構造 → どのようにネストされているか確認
- [ ] Wait アクション → クラス名確認
- [ ] `m_GUID` の型（整数 vs 文字列）確認
- [ ] アクションフィールドの実際の JSON キー名（`m_` プレフィックスの有無）確認

---

## 参考

- [ruby-macrodroid](https://github.com/jrobertson/ruby-macrodroid) — .mdr を解析できる Ruby gem
- MacroDroid フォーラム "Anatomy of the .mdr file"（要ログイン）
