using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AgentSkillsSample.WebApi.Sse;

/// <summary>
/// Redis 未使用時のフォールバック実装。
/// 単一プロセス内での SSE 再接続に対応（マルチインスタンス非対応）。
/// </summary>
public class InMemorySseEventStore : ISseEventStore
{
    private readonly ConcurrentDictionary<string, List<SseStoredEvent>> _events = new();
    private readonly ConcurrentDictionary<string, bool> _completed = new();

    public Task AppendAsync(string sessionId, string eventId, string data, CancellationToken ct = default)
    {
        _events.GetOrAdd(sessionId, _ => []).Add(new SseStoredEvent(eventId, data));
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<SseStoredEvent> GetEventsAfterAsync(
        string sessionId,
        string afterEventId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_events.TryGetValue(sessionId, out var events))
            yield break;

        bool found = false;
        foreach (var e in events)
        {
            if (!found)
            {
                if (e.Id == afterEventId) found = true;
                continue;
            }
            yield return e;
        }
        await Task.CompletedTask;
    }

    public Task<bool> IsCompletedAsync(string sessionId, CancellationToken ct = default) =>
        Task.FromResult(_completed.ContainsKey(sessionId));

    public Task MarkCompletedAsync(string sessionId, CancellationToken ct = default)
    {
        _completed[sessionId] = true;
        return Task.CompletedTask;
    }
}
