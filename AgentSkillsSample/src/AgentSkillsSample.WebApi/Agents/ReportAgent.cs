using AgentSkillsSample.WebApi.Skills;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// ③分析・文書化担当エージェント。
/// data-analysis / document-generation スキルで最終レポートを生成する。
/// </summary>
public class ReportAgent(DbSkillsProvider skillsProvider)
    : AgentBase(skillsProvider, "report-agent")
{
    public async Task<AgentReport> RunAsync(string verifiedResult, CancellationToken ct = default)
    {
        try
        {
            var result = await RunWithSkillsAsync(verifiedResult, ct: ct);
            return ToReport(AgentId, result);
        }
        catch (Exception ex)
        {
            return ToErrorReport(AgentId, ex);
        }
    }
}
