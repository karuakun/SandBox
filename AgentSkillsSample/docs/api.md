# API リファレンス

## エンドポイント一覧

| メソッド | パス | 説明 |
|---|---|---|
| `POST` | `/api/chat` | SSE ストリーミングチャット |
| `POST` | `/api/messages` | Bot Protocol（Microsoft.AgentFramework） |
| `GET` | `/openapi/v1.json` | OpenAPI スキーマ（開発環境のみ） |

---

## POST /api/chat

SSE（Server-Sent Events）形式でスーパーバイザーエージェントの処理をストリーミングします。

### リクエスト

```
Content-Type: application/json
```

```json
{
  "message": "AIエージェントの最新トレンドについて教えてください",
  "sessionId": "abc123"
}
```

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `message` | string | ✓ | ユーザーへの質問・指示 |
| `sessionId` | string | - | セッションID。省略時はサーバーが UUID を生成 |

### レスポンス

```
Content-Type: text/event-stream; charset=utf-8
Cache-Control: no-cache
Connection: keep-alive
X-Accel-Buffering: no
```

SSE イベント形式でチャンクが順次配信されます:

```
id: abc123:0
data: [supervisor] 🔍 ResearchAgent を呼び出し中...

id: abc123:1
data: [supervisor] 🔎 FactCheckAgent を呼び出し中...

id: abc123:2
data: [supervisor] 📝 ReportAgent を呼び出し中...

id: abc123:3
data: [supervisor] ✅ ValidationAgent で品質検証中...

id: abc123:4
data: [supervisor] 判定: Complete — 要件が充足されています

id: abc123:5
data: [supervisor] ✅ 要求を充足。最終レポートを出力します。

id: abc123:6
data: # AIエージェント最新トレンド

id: abc123:7
data: ## エグゼクティブサマリー
data: AIエージェントは2025年以降急速に普及しており...

data: [DONE]
```

- `id: {sessionId}:{index}` — イベント識別子（再接続時の再送に使用）
- `data: ...` — コンテンツ（改行は複数の `data:` 行に分割）
- `data: [DONE]` — ストリーム終了シグナル

---

## SSE 再接続フロー

クライアントが切断した場合、`Last-Event-ID` ヘッダーを付けて再接続することで、未受信イベントを再送できます。

```
1. クライアントが id: abc123:3 まで受信して切断

2. クライアントが再接続
   POST /api/chat
   Last-Event-ID: abc123:3
   { "message": "...", "sessionId": "abc123" }

3. サーバーが abc123:3 以降のイベントを再送
   id: abc123:4
   data: [supervisor] 判定: Complete — ...

   id: abc123:5
   ...

4. 既に完了していた場合は [DONE] のみ送信して終了
```

### ISseEventStore インターフェース

```csharp
public interface ISseEventStore
{
    Task AppendAsync(string sessionId, string eventId, string data, CancellationToken ct = default);
    IAsyncEnumerable<SseStoredEvent> GetEventsAfterAsync(string sessionId, string afterEventId, CancellationToken ct = default);
    Task<bool> IsCompletedAsync(string sessionId, CancellationToken ct = default);
    Task MarkCompletedAsync(string sessionId, CancellationToken ct = default);
}
```

### バックエンドの切り替え

`appsettings.json` の `Redis:Enabled` フラグで切り替えます:

| 設定 | 実装 | 特徴 |
|---|---|---|
| `false`（デフォルト） | `InMemorySseEventStore` | プロセス内メモリ保存。シングルインスタンス向け |
| `true` | `RedisSseEventStore` | Redis List + Key で永続化。マルチインスタンス対応 |

**Redis データ構造:**
- `sse:{sessionId}:events` — Redis List（イベント JSON を RPUSH）
- `sse:{sessionId}:done` — Redis String（"1"、完了フラグ）
- TTL: `Redis:SseEventTtlSeconds`（デフォルト 300 秒）

---

## POST /api/messages

Microsoft.AgentFramework の Bot Protocol エンドポイント。Activity Protocol（JSON）を受け付け、`SupervisorAgent.OnTurnAsync()` を呼び出します。

Teams や Bot Framework Emulator などの Bot クライアントから利用できます。

---

## curl サンプル

### 基本チャット

```bash
curl -N -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "量子コンピュータの現状と将来展望を教えてください"}'
```

### セッションID指定

```bash
curl -N -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "...", "sessionId": "my-session-001"}'
```

### 再接続（未受信イベントの再送）

```bash
curl -N -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -H "Last-Event-ID: my-session-001:3" \
  -d '{"message": "...", "sessionId": "my-session-001"}'
```

---

## エラーレスポンス

処理中にエラーが発生した場合、SSE ストリームに以下のようなメッセージが含まれます:

```
data: [supervisor] ⚠️ ResearchAgent 失敗: <エラーメッセージ>

data: [supervisor] ❌ 処理を中断します。理由: <理由>

data: [DONE]
```

ValidationAgent が正常に JSON を返せなかった場合は `Abort` 判定となり処理を終了します。
