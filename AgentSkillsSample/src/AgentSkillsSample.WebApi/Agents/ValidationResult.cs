namespace AgentSkillsSample.WebApi.Agents;

public enum ValidationVerdict { Complete, Retry, Abort }

/// <summary>ValidationAgent がスーパーバイザーへ返す判定結果</summary>
public record ValidationResult(
    ValidationVerdict Verdict,
    string Reason,
    string? RetryTarget = null,
    string? RetryInstruction = null
);
