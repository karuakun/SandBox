using Microsoft.Extensions.AI;

namespace AgentSkillsSample.WebApi.Skills;

/// <summary>
/// Microsoft.AgentFramework の FileAgentSkillsProvider に相当する DB 実装。
/// スキル定義を SQLite DB から動的にロードし、LLM へのプログレッシブ・ディスクロージャーを実現する。
/// </summary>
public class DbSkillsProvider(
    ISkillsRepository skillsRepo,
    IAgentRepository agentRepo,
    IServiceProvider sp,
    string agentId)
{
    private AgentDefinition? _cachedAgent;
    private IReadOnlyList<SkillDefinition>? _cachedSkills;

    // ──────────────────────────────────────────────────────────────
    // エージェント定義
    // ──────────────────────────────────────────────────────────────

    /// <summary>DB からエージェントのシステムプロンプトを取得する（広告フェーズで使用）</summary>
    public async Task<string> GetSystemPromptAsync(CancellationToken ct = default)
    {
        _cachedAgent ??= await agentRepo.GetByAgentIdAsync(agentId, ct)
            ?? throw new InvalidOperationException($"Agent '{agentId}' not found in database.");
        return _cachedAgent.SystemPrompt;
    }

    // ──────────────────────────────────────────────────────────────
    // スキル広告フェーズ（~100 token / スキル）
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// エージェントに登録されたスキルの名前と説明のみを返す。
    /// LLM のシステムプロンプトに追記してスキルの存在を「広告」する。
    /// </summary>
    public async Task<IReadOnlyList<SkillAdvertisement>> GetSkillAdvertisementsAsync(CancellationToken ct = default)
    {
        _cachedSkills ??= await skillsRepo.GetByAgentIdAsync(agentId, ct);
        return _cachedSkills.Select(s => new SkillAdvertisement(s.Name, s.Description)).ToList();
    }

    // ──────────────────────────────────────────────────────────────
    // スキルロードフェーズ（<5000 token / スキル）
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// スキル名を指定してフル Content（SKILL.md 形式）を返す。
    /// LLM が load_skill ツールを要求したときに呼び出す。
    /// </summary>
    public async Task<string> LoadSkillContentAsync(string skillName, CancellationToken ct = default)
    {
        var skill = await skillsRepo.GetByNameAsync(skillName, ct)
            ?? throw new KeyNotFoundException($"Skill '{skillName}' not found.");
        return skill.Content;
    }

    // ──────────────────────────────────────────────────────────────
    // モデル解決（スキル → エージェント → appsettings デフォルト）
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// スキルの ModelKey → エージェントの DefaultModelKey → "default" の優先順位で IChatClient を解決する。
    /// </summary>
    public IChatClient ResolveChatClient(string? skillModelKey, CancellationToken ct = default)
    {
        var keyedSp = sp.GetRequiredService<IKeyedServiceProvider>();

        // 1. スキル固有の ModelKey
        if (!string.IsNullOrEmpty(skillModelKey))
        {
            var client = keyedSp.GetKeyedService<IChatClient>(skillModelKey);
            if (client != null) return client;
        }

        // 2. エージェントの DefaultModelKey
        var agentModelKey = _cachedAgent?.DefaultModelKey;
        if (!string.IsNullOrEmpty(agentModelKey))
        {
            var client = keyedSp.GetKeyedService<IChatClient>(agentModelKey);
            if (client != null) return client;
        }

        // 3. DI のデフォルト IChatClient
        return sp.GetRequiredService<IChatClient>();
    }

    /// <summary>スキル名からモデルキーを解決してクライアントを返す</summary>
    public async Task<IChatClient> ResolveChatClientForSkillAsync(string? skillName, CancellationToken ct = default)
    {
        if (skillName == null) return ResolveChatClient(null);
        var skill = await skillsRepo.GetByNameAsync(skillName, ct);
        return ResolveChatClient(skill?.ModelKey);
    }

    // ──────────────────────────────────────────────────────────────
    // ヘルパー: スキル広告文字列の構築
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// システムプロンプトに追記するスキル広告テキストを生成する。
    /// LLM は `load_skill("skill-name")` を呼び出すことでフルコンテンツを取得できる。
    /// </summary>
    public async Task<string> BuildSkillsAdvertisementTextAsync(CancellationToken ct = default)
    {
        var skills = await GetSkillAdvertisementsAsync(ct);
        if (!skills.Any()) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine("## 利用可能なスキル");
        sb.AppendLine("以下のスキルが利用可能です。必要に応じて `load_skill(\"skill-name\")` でフル内容をロードしてください:");
        sb.AppendLine();
        foreach (var skill in skills)
        {
            sb.AppendLine($"- **{skill.Name}**: {skill.Description}");
        }
        return sb.ToString();
    }
}
