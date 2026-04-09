namespace AgentSkillsSample.WebApi.Agents;

/// <summary>各サブエージェントがスーパーバイザーへ返す処理報告</summary>
public record AgentReport(
    string AgentId,
    string Result,
    bool   Success,
    string? Notes = null
);
