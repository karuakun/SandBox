using AgentSkillsSample.WebApi.Skills;
using Microsoft.Extensions.AI;

namespace AgentSkillsSample.WebApi.Agents;

/// <summary>
/// サブエージェント共通基底クラス。
/// DB からシステムプロンプト・スキル広告をロードし、
/// Bedrock 経由で Claude を呼び出す。
/// </summary>
public abstract class AgentBase(DbSkillsProvider skillsProvider, string agentId)
{
    protected readonly DbSkillsProvider SkillsProvider = skillsProvider;
    protected readonly string AgentId = agentId;

    /// <summary>
    /// システムプロンプト + スキル広告 + ユーザー入力を組み合わせて
    /// IChatClient でメッセージを送信し、応答テキストを返す。
    /// LLM が load_skill ツールを要求した場合はスキルコンテンツを注入して再呼び出しする。
    /// </summary>
    protected async Task<string> RunWithSkillsAsync(
        string userInput,
        string? additionalSystemContext = null,
        CancellationToken ct = default)
    {
        // 1. DB からシステムプロンプトとスキル広告を取得
        var systemPrompt     = await SkillsProvider.GetSystemPromptAsync(ct);
        var skillsAdText     = await SkillsProvider.BuildSkillsAdvertisementTextAsync(ct);
        var fullSystemPrompt = systemPrompt + skillsAdText;

        if (!string.IsNullOrEmpty(additionalSystemContext))
            fullSystemPrompt += "\n\n" + additionalSystemContext;

        // 2. デフォルト IChatClient を解決（スキルなし）
        var chatClient = SkillsProvider.ResolveChatClient(null);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, fullSystemPrompt),
            new(ChatRole.User, userInput)
        };

        // 3. 最大2ラウンド（スキルロードを1回許容）
        for (int round = 0; round < 2; round++)
        {
            var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
            var text     = response.Text ?? string.Empty;

            // load_skill("skill-name") パターンを検出
            var skillName = ExtractLoadSkillRequest(text);
            if (skillName == null)
                return text;

            // スキルをロードしてコンテキストに追加
            var skillContent = await SkillsProvider.LoadSkillContentAsync(skillName, ct);
            chatClient = await SkillsProvider.ResolveChatClientForSkillAsync(skillName, ct);

            messages.Add(new ChatMessage(ChatRole.Assistant, text));
            messages.Add(new ChatMessage(ChatRole.User,
                $"[スキルロード完了] {skillName}:\n\n{skillContent}\n\n上記スキルを使って再度回答してください。"));
        }

        // フォールバック（ループ上限）
        var final = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return final.Text ?? string.Empty;
    }

    /// <summary>テキストから load_skill("name") 呼び出しを抽出する</summary>
    private static string? ExtractLoadSkillRequest(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"load_skill\([""']([^""']+)[""']\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>エージェントIDと結果を AgentReport に包む</summary>
    protected static AgentReport ToReport(string agentId, string result, string? notes = null) =>
        new(agentId, result, Success: true, Notes: notes);

    protected static AgentReport ToErrorReport(string agentId, Exception ex) =>
        new(agentId, $"[エラー] {ex.Message}", Success: false, Notes: ex.GetType().Name);
}
