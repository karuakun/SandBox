using System.Runtime.CompilerServices;
using System.Text;
using AgentSkillsSample.WebApi.Agents;
using AgentSkillsSample.WebApi.Sse;

namespace AgentSkillsSample.WebApi.Endpoints;

public record ChatRequest(string Message, string? SessionId = null);

public static class ChatEndpoints
{
    public static WebApplication MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chat", HandleChatAsync)
           .WithName("Chat")
           .WithOpenApi();

        return app;
    }

    private static async Task HandleChatAsync(
        ChatRequest     req,
        HttpContext     ctx,
        SupervisorAgent supervisor,
        ISseEventStore  eventStore)
    {
        var sessionId   = req.SessionId ?? Guid.NewGuid().ToString("N");
        var lastEventId = ctx.Request.Headers["Last-Event-ID"].FirstOrDefault();
        var ct          = ctx.RequestAborted;

        ctx.Response.Headers.Append("Content-Type", "text/event-stream; charset=utf-8");
        ctx.Response.Headers.Append("Cache-Control", "no-cache");
        ctx.Response.Headers.Append("Connection", "keep-alive");
        ctx.Response.Headers.Append("X-Accel-Buffering", "no");

        long eventIndex = 0;

        // ── 再接続時: 未受信イベントを replay ──
        if (lastEventId != null)
        {
            await foreach (var stored in eventStore.GetEventsAfterAsync(sessionId, lastEventId, ct))
            {
                await WriteSseEventAsync(ctx.Response, stored.Id, stored.Data, ct);
            }

            if (await eventStore.IsCompletedAsync(sessionId, ct))
            {
                await WriteSseEventAsync(ctx.Response, null, "[DONE]", ct);
                return;
            }
        }

        // ── 通常ストリーミング ──
        await foreach (var chunk in supervisor.StreamAsync(req.Message, ct))
        {
            var id = $"{sessionId}:{eventIndex++}";
            await eventStore.AppendAsync(sessionId, id, chunk, ct);
            await WriteSseEventAsync(ctx.Response, id, chunk, ct);
        }

        await eventStore.MarkCompletedAsync(sessionId, ct);
        await WriteSseEventAsync(ctx.Response, null, "[DONE]", ct);
    }

    private static async Task WriteSseEventAsync(HttpResponse response, string? eventId, string data, CancellationToken ct)
    {
        var sb = new StringBuilder();
        if (eventId != null) sb.AppendLine($"id: {eventId}");
        // data: フィールドは改行ごとに分割して送信
        foreach (var line in data.Split('\n'))
            sb.AppendLine($"data: {line}");
        sb.AppendLine(); // イベント終端の空行

        await response.WriteAsync(sb.ToString(), Encoding.UTF8, ct);
        await response.Body.FlushAsync(ct);
    }
}
