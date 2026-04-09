namespace AgentSkillsSample.WebApi.Skills;

public interface ISkillsRepository
{
    Task<IReadOnlyList<SkillDefinition>> GetByAgentIdAsync(string agentId, CancellationToken ct = default);
    Task<SkillDefinition?> GetByNameAsync(string name, CancellationToken ct = default);
}
