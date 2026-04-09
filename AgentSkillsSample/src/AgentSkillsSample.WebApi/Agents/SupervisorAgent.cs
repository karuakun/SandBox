using System.Runtime.CompilerServices;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// スーパーバイザーエージェント。
/// Microsoft.AgentFramework の IAgent を実装し、
/// ResearchAgent → FactCheckAgent → ReportAgent → ValidationAgent の
/// ループオーケストレーションを担う。
/// ValidationAgent の判定により「完了 / リトライ / 中断」を決定する。
/// </summary>
public class SupervisorAgent(
    ResearchAgent   researchAgent,
    FactCheckAgent  factCheckAgent,
    ReportAgent     reportAgent,
    ValidationAgent validationAgent) : IAgent
{
    private const int MaxRetries = 3;

    // ──────────────────────────────────────────────────────────────
    // IAgent 実装（Microsoft.AgentFramework 統合）
    // ──────────────────────────────────────────────────────────────

    public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        if (turnContext.Activity.Type != ActivityTypes.Message)
            return;

        var userMessage = turnContext.Activity.Text ?? string.Empty;

        await foreach (var chunk in StreamAsync(userMessage, cancellationToken))
        {
            // SSE チャンクを Activity として送信
            await turnContext.SendActivityAsync(MessageFactory.Text(chunk), cancellationToken);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // SSE ストリーミング（ChatEndpoints から直接呼び出し）
    // ──────────────────────────────────────────────────────────────

    public async IAsyncEnumerable<string> StreamAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var currentInput = userMessage;
        var retryCount   = 0;

        while (retryCount <= MaxRetries)
        {
            var reports = new List<AgentReport>();

            // ① ResearchAgent
            yield return $"[supervisor] 🔍 ResearchAgent を呼び出し中...\n";
            var r1 = await researchAgent.RunAsync(currentInput, ct);
            reports.Add(r1);
            if (!r1.Success)
            {
                yield return $"[supervisor] ⚠️ ResearchAgent 失敗: {r1.Notes}\n";
            }

            // ② FactCheckAgent
            yield return $"[supervisor] 🔎 FactCheckAgent を呼び出し中...\n";
            var r2 = await factCheckAgent.RunAsync(r1.Result, ct);
            reports.Add(r2);
            if (!r2.Success)
            {
                yield return $"[supervisor] ⚠️ FactCheckAgent 失敗: {r2.Notes}\n";
            }

            // ③ ReportAgent
            yield return $"[supervisor] 📝 ReportAgent を呼び出し中...\n";
            var r3 = await reportAgent.RunAsync(r2.Result, ct);
            reports.Add(r3);

            // ④ ValidationAgent
            yield return $"[supervisor] ✅ ValidationAgent で品質検証中...\n";
            var validation = await validationAgent.ValidateAsync(userMessage, reports, ct);

            yield return $"[supervisor] 判定: {validation.Verdict} — {validation.Reason}\n";

            if (validation.Verdict == ValidationVerdict.Complete)
            {
                yield return "[supervisor] ✅ 要求を充足。最終レポートを出力します。\n\n";
                yield return r3.Result;
                yield break;
            }

            if (validation.Verdict == ValidationVerdict.Abort || retryCount >= MaxRetries)
            {
                yield return $"[supervisor] ❌ 処理を中断します。理由: {validation.Reason}\n";
                yield break;
            }

            // Retry
            retryCount++;
            yield return $"[supervisor] 🔄 リトライ {retryCount}/{MaxRetries}: {validation.RetryInstruction ?? "再調査します"}\n\n";
            currentInput = validation.RetryInstruction is not null
                ? $"{userMessage}\n\n[追加指示] {validation.RetryInstruction}"
                : userMessage;
        }
    }
}
