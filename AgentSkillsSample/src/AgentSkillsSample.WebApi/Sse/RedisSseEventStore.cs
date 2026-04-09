using System.Runtime.CompilerServices;
using System.Text.Json;
using StackExchange.Redis;

namespace AgentSkillsSample.WebApi.Sse;

/// <summary>
/// Redis を使った SSE イベントストア。
/// マルチインスタンス環境での再接続に対応。
/// </summary>
public class RedisSseEventStore(IConnectionMultiplexer redis, IConfiguration config) : ISseEventStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(
        config.GetValue("Redis:SseEventTtlSeconds", 300));

    private static string EventsKey(string sessionId) => $"sse:{sessionId}:events";
    private static string DoneKey(string sessionId)   => $"sse:{sessionId}:done";

    public async Task AppendAsync(string sessionId, string eventId, string data, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new SseStoredEvent(eventId, data));
        var key  = EventsKey(sessionId);
        await _db.ListRightPushAsync(key, json);
        await _db.KeyExpireAsync(key, _ttl);
    }

    public async IAsyncEnumerable<SseStoredEvent> GetEventsAfterAsync(
        string sessionId,
        string afterEventId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var all = await _db.ListRangeAsync(EventsKey(sessionId));
        bool found = false;

        foreach (var item in all)
        {
            var e = JsonSerializer.Deserialize<SseStoredEvent>((string)item!);
            if (e is null) continue;

            if (!found)
            {
                if (e.Id == afterEventId) found = true;
                continue;
            }
            yield return e;
        }
    }

    public async Task<bool> IsCompletedAsync(string sessionId, CancellationToken ct = default) =>
        await _db.KeyExistsAsync(DoneKey(sessionId));

    public async Task MarkCompletedAsync(string sessionId, CancellationToken ct = default)
    {
        var key = DoneKey(sessionId);
        await _db.StringSetAsync(key, "1", _ttl);
    }
}
