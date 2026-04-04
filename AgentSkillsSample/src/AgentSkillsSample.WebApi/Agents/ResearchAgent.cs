using AgentSkillsSample.WebApi.Skills;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// ①情報収集・要約担当エージェント。
/// web-search / text-summarization スキルを使いユーザーの質問を調査する。
/// </summary>
public class ResearchAgent(DbSkillsProvider skillsProvider)
    : AgentBase(skillsProvider, "research-agent")
{
    public async Task<AgentReport> RunAsync(string userMessage, CancellationToken ct = default)
    {
        try
        {
            var result = await RunWithSkillsAsync(userMessage, ct: ct);
            return ToReport(AgentId, result);
        }
        catch (Exception ex)
        {
            return ToErrorReport(AgentId, ex);
        }
    }
}
