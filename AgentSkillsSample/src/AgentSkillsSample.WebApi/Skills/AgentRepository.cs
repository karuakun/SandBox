using AgentSkillsSample.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AgentSkillsSample.WebApi.Skills;

public class AgentRepository(AgentDbContext db) : IAgentRepository
{
    public Task<AgentDefinition?> GetByAgentIdAsync(string agentId, CancellationToken ct = default) =>
        db.Agents
          .AsNoTracking()
          .FirstOrDefaultAsync(a => a.AgentId == agentId && a.IsActive, ct);
}
