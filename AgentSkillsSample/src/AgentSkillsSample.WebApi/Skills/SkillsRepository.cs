using AgentSkillsSample.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AgentSkillsSample.WebApi.Skills;

public class SkillsRepository(AgentDbContext db) : ISkillsRepository
{
    public async Task<IReadOnlyList<SkillDefinition>> GetByAgentIdAsync(string agentId, CancellationToken ct = default) =>
        await db.AgentSkillMappings
                .AsNoTracking()
                .Where(m => m.AgentId == agentId)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Skill)
                .Where(s => s.IsActive)
                .ToListAsync(ct);

    public Task<SkillDefinition?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Skills
          .AsNoTracking()
          .FirstOrDefaultAsync(s => s.Name == name && s.IsActive, ct);
}
