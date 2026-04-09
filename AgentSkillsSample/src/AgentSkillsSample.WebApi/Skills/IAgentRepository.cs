namespace AgentSkillsSample.WebApi.Skills;

public interface IAgentRepository
{
    Task<AgentDefinition?> GetByAgentIdAsync(string agentId, CancellationToken ct = default);
}
