namespace AgentSkillsSample.WebApi.Sse;

public record SseStoredEvent(string Id, string Data);

/// <summary>
/// SSE の再接続時にイベントを replay するためのストア。
/// クライアントが Last-Event-ID を送信して再接続した場合に、
/// 未受信イベントを返す。
/// </summary>
public interface ISseEventStore
{
    Task AppendAsync(string sessionId, string eventId, string data, CancellationToken ct = default);
    IAsyncEnumerable<SseStoredEvent> GetEventsAfterAsync(string sessionId, string afterEventId, CancellationToken ct = default);
    Task<bool> IsCompletedAsync(string sessionId, CancellationToken ct = default);
    Task MarkCompletedAsync(string sessionId, CancellationToken ct = default);
}
