using AgentSkillsSample.WebApi.Skills;
using Microsoft.EntityFrameworkCore;

namespace AgentSkillsSample.WebApi.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AgentDbContext db)
    {
        if (await db.Agents.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // ────────────────────────────────
        // エージェント定義
        // ────────────────────────────────
        var agents = new List<AgentDefinition>
        {
            new()
            {
                AgentId = "research-agent",
                Name = "ResearchAgent",
                Description = "ユーザーの質問に関する情報を収集・整理するエージェント",
                SystemPrompt = """
                    あなたは優秀なリサーチアシスタントです。
                    ユーザーの質問に対して、関連する情報を幅広く収集し、正確にまとめてください。
                    情報は箇条書きや構造化された形式で整理してください。
                    不明な点は不明と明記し、確実な情報のみを提供してください。
                    利用可能なスキルを確認し、必要に応じてスキルをロードして活用してください。
                    """,
                DefaultModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                AgentId = "fact-check-agent",
                Name = "FactCheckAgent",
                Description = "収集された情報の事実確認・信頼性評価を行うエージェント",
                SystemPrompt = """
                    あなたはファクトチェックの専門家です。
                    提供された情報の正確性・信頼性・論理的一貫性を厳密に評価してください。
                    各主張について: (1)確認済み (2)要確認 (3)誤情報の可能性あり のいずれかで分類してください。
                    根拠が弱い点や矛盾点を明確に指摘し、信頼性スコア（0-100）も付与してください。
                    利用可能なスキルを確認し、必要に応じてスキルをロードして活用してください。
                    """,
                DefaultModelKey = "opus",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                AgentId = "report-agent",
                Name = "ReportAgent",
                Description = "検証済み情報をレポート文書として整形・出力するエージェント",
                SystemPrompt = """
                    あなたは優秀なテクニカルライターです。
                    提供された情報を、読みやすく構造化されたレポートとして作成してください。
                    レポートは以下の形式で記述してください:
                    - ## 概要: 主要ポイントの要約（3-5文）
                    - ## 詳細: 各トピックの詳細説明
                    - ## 結論: 重要な発見と示唆
                    Markdown形式を使用し、見出し・箇条書き・強調を適切に活用してください。
                    利用可能なスキルを確認し、必要に応じてスキルをロードして活用してください。
                    """,
                DefaultModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                AgentId = "validation-agent",
                Name = "ValidationAgent",
                Description = "ユーザー要求の充足度を評価し、スーパーバイザーへ判定を返すエージェント",
                SystemPrompt = """
                    あなたは品質検証の専門家です。
                    ユーザーの元のリクエストと、各エージェントが生成した出力を比較評価してください。
                    以下の基準で評価し、必ず次のJSON形式のみで回答してください（他のテキストは不要）:

                    {
                      "verdict": "Complete" | "Retry" | "Abort",
                      "reason": "判定理由の説明",
                      "retryTarget": "retry時: 再実行すべきエージェントID（research-agent/fact-check-agent/report-agent）",
                      "retryInstruction": "retry時: 改善のための追加指示"
                    }

                    Complete: ユーザーの要求が十分に満たされている
                    Retry: 品質・網羅性に問題があり改善可能
                    Abort: 要求が不明確または処理継続が不可能
                    """,
                DefaultModelKey = "opus",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.Agents.AddRange(agents);

        // ────────────────────────────────
        // スキル定義
        // ────────────────────────────────
        var skills = new List<SkillDefinition>
        {
            new()
            {
                Name = "web-search",
                Description = "Web情報検索スキル: 最新情報の収集手順と出力形式を定義します",
                Content = """
                    # Web Search Skill

                    ## 目的
                    指定されたトピックに関するWeb上の最新情報を体系的に収集する。

                    ## 手順
                    1. 検索クエリを3-5個作成（異なる角度から）
                    2. 各クエリに対して想定される情報源を特定（公式サイト、学術論文、ニュース等）
                    3. 情報の新鮮度を考慮し、最新（2024-2026年）の情報を優先する
                    4. 収集した情報を信頼性順にランキング

                    ## 出力形式
                    - 情報源名と想定URL
                    - 収集した主要情報（箇条書き）
                    - 情報の確実性レベル（高/中/低）
                    """,
                ModelKey = "haiku",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "text-summarization",
                Description = "テキスト要約スキル: 長文を構造化して要約する手法を定義します",
                Content = """
                    # Text Summarization Skill

                    ## 目的
                    収集した大量のテキスト情報を、重要度に基づいて構造化・要約する。

                    ## 手順
                    1. テキスト全体を読み、主要テーマを3-7個特定する
                    2. 各テーマについて最も重要な情報を抽出（1テーマあたり最大3文）
                    3. 繰り返しの情報を排除し、ユニークな洞察を優先する
                    4. 抽出した情報を論理的な順序で並び替える

                    ## 出力形式
                    - **要約（3-5文）**: 最重要ポイントのみ
                    - **詳細リスト**: テーマ別に構造化した情報
                    - **キーワード**: 重要用語のリスト（最大10個）
                    """,
                ModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "source-verification",
                Description = "情報ソース検証スキル: 情報源の信頼性と正確性を検証する手法を定義します",
                Content = """
                    # Source Verification Skill

                    ## 目的
                    提供された情報の出典・信頼性・正確性を体系的に検証する。

                    ## 検証基準
                    1. **権威性**: 情報源が分野の専門機関・専門家か
                    2. **正確性**: 事実関係に誤りや誇張がないか
                    3. **最新性**: 情報が最新（2024-2026年）か、古い情報でないか
                    4. **一致性**: 複数の独立した情報源で確認できるか

                    ## 評価スコア
                    各情報に 0-100 のスコアを付与:
                    - 80-100: 高信頼性（複数の権威ある情報源で確認済み）
                    - 60-79: 中信頼性（単一の信頼できる情報源）
                    - 40-59: 要注意（情報源不明確または古い）
                    - 0-39: 低信頼性（確認不可または矛盾あり）

                    ## 出力形式
                    各主張について: 信頼性スコア + 判定理由 + 推奨アクション
                    """,
                ModelKey = "opus",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "claim-analysis",
                Description = "主張分析スキル: 論理的整合性と根拠の妥当性を分析します",
                Content = """
                    # Claim Analysis Skill

                    ## 目的
                    提示された主張の論理的整合性・根拠の妥当性・反証可能性を分析する。

                    ## 分析手順
                    1. 主張を「前提」と「結論」に分解する
                    2. 前提が結論を支持するか論理的に検証する
                    3. 反論や例外ケースを検討する
                    4. 根拠の強さを評価する（実証的/理論的/推測的）

                    ## 出力形式
                    - **主張の要約**: 1文で
                    - **論理構造**: 前提→結論の流れ
                    - **強み**: 支持する根拠
                    - **弱み/反論**: 問題点や反証
                    - **総合評価**: 強い/中程度/弱い
                    """,
                ModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "data-analysis",
                Description = "データ分析スキル: 情報の比較・パターン抽出・統計的評価を行います",
                Content = """
                    # Data Analysis Skill

                    ## 目的
                    収集・検証された情報を分析し、意味のあるパターンや洞察を抽出する。

                    ## 分析手法
                    1. **比較分析**: 複数の情報・立場・アプローチを比較する
                    2. **トレンド分析**: 時系列での変化・傾向を特定する
                    3. **因果分析**: 原因と結果の関係を特定する
                    4. **ギャップ分析**: 現状と理想の差を特定する

                    ## 出力形式
                    - **主要発見事項**: 最も重要な3-5点
                    - **比較表**: 異なる選択肢やアプローチの比較（該当する場合）
                    - **トレンド**: 観察された変化や傾向
                    - **示唆点**: 分析から導かれる推奨事項
                    """,
                ModelKey = "opus",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "document-generation",
                Description = "文書生成スキル: 分析結果を読みやすいレポートとして整形します",
                Content = """
                    # Document Generation Skill

                    ## 目的
                    分析結果を、対象読者に適した形式の文書として生成する。

                    ## 文書構造
                    ```
                    # [タイトル]

                    ## エグゼクティブサマリー
                    （3-5文で主要ポイントを要約）

                    ## 背景・文脈
                    （トピックの背景説明）

                    ## 主要発見事項
                    ### 1. [発見事項1]
                    ### 2. [発見事項2]
                    ...

                    ## 分析と考察
                    （詳細な分析内容）

                    ## 結論と推奨事項
                    （行動可能な提言）
                    ```

                    ## スタイルガイド
                    - 専門用語は初出時に説明を付ける
                    - 能動態を優先する
                    - 箇条書きと本文を適切に混在させる
                    - 重要な数値や用語は**太字**で強調
                    """,
                ModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "requirement-check",
                Description = "要件確認スキル: ユーザー要求と出力内容の対応を確認します",
                Content = """
                    # Requirement Check Skill

                    ## 目的
                    ユーザーの元のリクエストと、生成された出力を照合して充足度を評価する。

                    ## 確認手順
                    1. ユーザーリクエストから要件を抽出する（明示的・暗黙的）
                    2. 各要件が出力に含まれているか確認する
                    3. 要件の充足率を計算する（充足済み数 / 全要件数）
                    4. 未充足の要件を特定し優先順位を付ける

                    ## 判定基準
                    - **Complete（完了）**: 充足率 80%以上、かつ重要要件がすべて充足
                    - **Retry（再試行）**: 充足率 50-79%、または重要要件に未充足あり
                    - **Abort（中断）**: 充足率 50%未満、またはリクエストが実現不可能

                    ## 出力形式
                    要件リスト + 充足状況 + 総合充足率 + 推奨判定
                    """,
                ModelKey = "opus",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Name = "quality-assessment",
                Description = "品質評価スキル: 出力の完成度・正確性・有用性を総合評価します",
                Content = """
                    # Quality Assessment Skill

                    ## 目的
                    生成された出力の品質を多角的に評価し、改善点を特定する。

                    ## 評価軸
                    1. **正確性** (0-25点): 情報に誤りがないか
                    2. **完全性** (0-25点): 要求されたすべての情報が含まれているか
                    3. **明確性** (0-25点): 内容が分かりやすく構造化されているか
                    4. **有用性** (0-25点): ユーザーの目的達成に役立つか

                    ## 合計スコア判定
                    - 80-100: 高品質（Complete推奨）
                    - 60-79: 中品質（条件付きComplete または Retry推奨）
                    - 40-59: 低品質（Retry推奨）
                    - 0-39: 不合格（Abort推奨）

                    ## 出力形式
                    各評価軸のスコア + 理由 + 合計スコア + 具体的な改善提案
                    """,
                ModelKey = "sonnet",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.Skills.AddRange(skills);
        await db.SaveChangesAsync();

        // ────────────────────────────────
        // エージェント↔スキルマッピング
        // ────────────────────────────────
        var webSearch = skills.First(s => s.Name == "web-search");
        var textSum   = skills.First(s => s.Name == "text-summarization");
        var sourceVer = skills.First(s => s.Name == "source-verification");
        var claimAn   = skills.First(s => s.Name == "claim-analysis");
        var dataAn    = skills.First(s => s.Name == "data-analysis");
        var docGen    = skills.First(s => s.Name == "document-generation");
        var reqCheck  = skills.First(s => s.Name == "requirement-check");
        var qualAss   = skills.First(s => s.Name == "quality-assessment");

        var mappings = new List<AgentSkillMapping>
        {
            new() { AgentId = "research-agent",   SkillId = webSearch.Id, SortOrder = 1 },
            new() { AgentId = "research-agent",   SkillId = textSum.Id,   SortOrder = 2 },
            new() { AgentId = "fact-check-agent", SkillId = sourceVer.Id, SortOrder = 1 },
            new() { AgentId = "fact-check-agent", SkillId = claimAn.Id,   SortOrder = 2 },
            new() { AgentId = "report-agent",     SkillId = dataAn.Id,    SortOrder = 1 },
            new() { AgentId = "report-agent",     SkillId = docGen.Id,    SortOrder = 2 },
            new() { AgentId = "validation-agent", SkillId = reqCheck.Id,  SortOrder = 1 },
            new() { AgentId = "validation-agent", SkillId = qualAss.Id,   SortOrder = 2 },
        };

        db.AgentSkillMappings.AddRange(mappings);
        await db.SaveChangesAsync();
    }
}
