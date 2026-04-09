using AgentSkillsSample.WebApi.Skills;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// ②事実確認・検証担当エージェント。
/// source-verification / claim-analysis スキルで収集情報の信頼性を評価する。
/// </summary>
public class FactCheckAgent(DbSkillsProvider skillsProvider)
    : AgentBase(skillsProvider, "fact-check-agent")
{
    public async Task<AgentReport> RunAsync(string researchResult, CancellationToken ct = default)
    {
        try
        {
            var result = await RunWithSkillsAsync(researchResult, ct: ct);
            return ToReport(AgentId, result);
        }
        catch (Exception ex)
        {
            return ToErrorReport(AgentId, ex);
        }
    }
}
