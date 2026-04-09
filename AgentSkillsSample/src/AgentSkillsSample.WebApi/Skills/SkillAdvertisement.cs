namespace AgentSkillsSample.WebApi.Skills;

/// <summary>
/// スキルの広告フェーズで LLM に提示する情報（~100 token）
/// </summary>
public record SkillAdvertisement(string Name, string Description);
