using AgentSkillsSample.WebApi.Skills;
using Microsoft.EntityFrameworkCore;

namespace AgentSkillsSample.WebApi.Data;

public class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options)
{
    public DbSet<AgentDefinition> Agents => Set<AgentDefinition>();
    public DbSet<SkillDefinition> Skills => Set<SkillDefinition>();
    public DbSet<AgentSkillMapping> AgentSkillMappings => Set<AgentSkillMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentDefinition>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.AgentId).IsUnique();
            e.Property(a => a.AgentId).HasMaxLength(100).IsRequired();
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.SystemPrompt).IsRequired();
            e.Property(a => a.DefaultModelKey).HasMaxLength(50);
        });

        modelBuilder.Entity<SkillDefinition>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Name).IsUnique();
            e.Property(s => s.Name).HasMaxLength(64).IsRequired();
            e.Property(s => s.Description).HasMaxLength(1024).IsRequired();
            e.Property(s => s.Content).IsRequired();
            e.Property(s => s.ModelKey).HasMaxLength(50);
        });

        modelBuilder.Entity<AgentSkillMapping>(e =>
        {
            e.HasKey(m => new { m.AgentId, m.SkillId });
            e.Property(m => m.AgentId).HasMaxLength(100).IsRequired();
            e.HasOne(m => m.Skill)
             .WithMany(s => s.AgentMappings)
             .HasForeignKey(m => m.SkillId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
