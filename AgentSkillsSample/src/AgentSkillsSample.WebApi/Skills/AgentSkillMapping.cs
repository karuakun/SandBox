namespace AgentSkillsSample.WebApi.Skills;

public class AgentSkillMapping
{
    public string AgentId { get; set; } = string.Empty;
    public int SkillId { get; set; }
    public int SortOrder { get; set; }

    public SkillDefinition Skill { get; set; } = null!;
}
