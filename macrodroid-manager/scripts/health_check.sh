#!/data/data/com.termux/files/usr/bin/bash
# MacroDroid から呼び出される日次ヘルスチェックスクリプト

set -euo pipefail

VERBOSE=false
while [[ $# -gt 0 ]]; do
  case $1 in
    --verbose) VERBOSE=true; shift ;;
    *) shift ;;
  esac
done

log() { [[ "$VERBOSE" == true ]] && echo "[health_check] $*"; }

# バッテリー残量チェック
BATTERY=$(termux-battery-status 2>/dev/null | python3 -c "import sys,json; print(json.load(sys.stdin)['percentage'])" 2>/dev/null || echo "unknown")
log "バッテリー: ${BATTERY}%"

# ストレージ残量チェック（内部ストレージ）
STORAGE=$(df /data 2>/dev/null | awk 'NR==2 {print $5}' | tr -d '%' || echo "unknown")
log "ストレージ使用率: ${STORAGE}%"

# 異常判定
EXIT_CODE=0
if [[ "$BATTERY" != "unknown" ]] && (( BATTERY < 15 )); then
  log "警告: バッテリー残量低下 (${BATTERY}%)"
  EXIT_CODE=1
fi
if [[ "$STORAGE" != "unknown" ]] && (( STORAGE > 90 )); then
  log "警告: ストレージ残量不足 (${STORAGE}%使用)"
  EXIT_CODE=1
fi

# MacroDroid へ結果を渡す変数ファイル
echo "$EXIT_CODE" > /data/data/com.termux/files/home/macrodroid/.last_exit_code

exit $EXIT_CODE
