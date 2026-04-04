using System.Text.Json;
using AgentSkillsSample.WebApi.Skills;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// ④ユーザー要求充足度の検証エージェント。
/// requirement-check / quality-assessment スキルで出力を評価し、
/// Complete / Retry / Abort の判定を返す。
/// </summary>
public class ValidationAgent(DbSkillsProvider skillsProvider)
    : AgentBase(skillsProvider, "validation-agent")
{
    public async Task<ValidationResult> ValidateAsync(
        string originalRequest,
        IReadOnlyList<AgentReport> reports,
        CancellationToken ct = default)
    {
        var reportsText = string.Join("\n\n", reports.Select(r =>
            $"### {r.AgentId} (Success={r.Success})\n{r.Result}" +
            (r.Notes != null ? $"\n[Notes: {r.Notes}]" : "")));

        var input = $"""
            ## 元のユーザーリクエスト
            {originalRequest}

            ## 各エージェントの処理結果
            {reportsText}
            """;

        try
        {
            var text = await RunWithSkillsAsync(input, ct: ct);
            return ParseValidationResult(text);
        }
        catch (Exception ex)
        {
            return new ValidationResult(
                ValidationVerdict.Abort,
                $"ValidationAgent でエラーが発生しました: {ex.Message}");
        }
    }

    private static ValidationResult ParseValidationResult(string text)
    {
        // JSON ブロックを抽出（```json ... ``` または裸の JSON）
        var json = ExtractJson(text);
        if (json == null)
            return new ValidationResult(ValidationVerdict.Abort, $"ValidationAgent の応答を JSON としてパースできませんでした。\n\n{text}");

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var verdictStr = root.GetProperty("verdict").GetString() ?? "Abort";
            var verdict = verdictStr switch
            {
                "Complete" => ValidationVerdict.Complete,
                "Retry"    => ValidationVerdict.Retry,
                _          => ValidationVerdict.Abort
            };

            return new ValidationResult(
                verdict,
                root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                root.TryGetProperty("retryTarget", out var rt) ? rt.GetString() : null,
                root.TryGetProperty("retryInstruction", out var ri) ? ri.GetString() : null
            );
        }
        catch
        {
            return new ValidationResult(ValidationVerdict.Abort, $"JSON パースエラー:\n{json}");
        }
    }

    private static string? ExtractJson(string text)
    {
        // ```json ... ``` ブロック
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"```(?:json)?\s*(\{.*?\})\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        // 裸の JSON オブジェクト
        var start = text.IndexOf('{');
        var end   = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return null;
    }
}
