# エージェント設計

## オーケストレーションフロー

```
POST /api/chat {"message": "..."}
         │
         ▼
SupervisorAgent.StreamAsync()   ← 最大 3 回リトライループ
         │
         │  yield "[supervisor] 🔍 ResearchAgent を呼び出し中...\n"
         ├──► ResearchAgent.RunAsync(message)
         │         1. DB: Agents.SystemPrompt をロード
         │         2. DB: スキル広告 [web-search/haiku, text-summarization/sonnet]
         │         3. Bedrock 呼び出し（agent の DefaultModelKey = sonnet）
         │         4. LLM が load_skill("web-search") を含む場合:
         │              → DB: スキルの Content をロード（フル内容）
         │              → haiku クライアントで再呼び出し
         │         └─► AgentReport{AgentId, Result, Success, Notes}
         │
         │  yield "[supervisor] 🔎 FactCheckAgent を呼び出し中...\n"
         ├──► FactCheckAgent.RunAsync(researchResult)
         │         スキル: source-verification/opus, claim-analysis/sonnet
         │         DefaultModelKey = opus
         │         └─► AgentReport
         │
         │  yield "[supervisor] 📝 ReportAgent を呼び出し中...\n"
         ├──► ReportAgent.RunAsync(factCheckResult)
         │         スキル: data-analysis/opus, document-generation/sonnet
         │         DefaultModelKey = sonnet
         │         └─► AgentReport
         │
         │  yield "[supervisor] ✅ ValidationAgent で品質検証中...\n"
         └──► ValidationAgent.ValidateAsync(originalRequest, [r1, r2, r3])
                   スキル: requirement-check/opus, quality-assessment/sonnet
                   DefaultModelKey = opus
                   LLM 出力: JSON { verdict, reason, retryTarget, retryInstruction }
                   └─► ValidationResult{Verdict, Reason, RetryTarget?, RetryInstruction?}

  ┌── Complete ──► yield 最終レポート → SSE "[DONE]"
  ├── Retry    ──► retryCount++ → RetryInstruction を入力に追記してループ再開
  └── Abort / maxRetries 超過 ──► yield エラーメッセージ → SSE "[DONE]"
```

## スーパーバイザーの判定ロジック

```csharp
// SupervisorAgent.cs
while (retryCount <= MaxRetries)   // MaxRetries = 3
{
    var r1 = await researchAgent.RunAsync(currentInput);
    var r2 = await factCheckAgent.RunAsync(r1.Result);
    var r3 = await reportAgent.RunAsync(r2.Result);

    var validation = await validationAgent.ValidateAsync(userMessage, [r1, r2, r3]);

    if (validation.Verdict == ValidationVerdict.Complete)
    {
        // r3.Result を yield して完了
    }
    else if (validation.Verdict == ValidationVerdict.Abort || retryCount >= MaxRetries)
    {
        // エラーメッセージを yield して終了
    }
    else  // Retry
    {
        retryCount++;
        currentInput = RetryInstruction != null
            ? $"{userMessage}\n\n[追加指示] {validation.RetryInstruction}"
            : userMessage;
    }
}
```

## AgentBase — スキル動的ロードの仕組み

すべてのサブエージェントは `AgentBase` を継承し、`RunWithSkillsAsync()` を通じてスキルを活用します。

### プログレッシブ・ディスクロージャーパターン

```
フェーズ 1: 広告（~100 token / スキル）
  ┌─────────────────────────────────────────────────────┐
  │ システムプロンプト（DB: Agents.SystemPrompt）        │
  │                                                      │
  │ ## 利用可能なスキル                                  │
  │ - web-search: Web情報検索スキル: ...                 │
  │ - text-summarization: テキスト要約スキル: ...        │
  │                                                      │
  │ 必要に応じて load_skill("skill-name") でロード可能   │
  └─────────────────────────────────────────────────────┘
                    │
                    ▼ LLM が load_skill("web-search") を含む応答を返す
                    │
フェーズ 2: ロード（<5000 token / スキル）
  ┌─────────────────────────────────────────────────────┐
  │ DB から Skills.Content（SKILL.md 形式）を取得         │
  │ スキルの ModelKey に応じた IChatClient を解決         │
  │ [スキルロード完了] web-search:                       │
  │ # Web Search Skill                                   │
  │ ## 手順: 1. 検索クエリを3-5個...                     │
  └─────────────────────────────────────────────────────┘
                    │
                    ▼ スキル内容を含めて LLM を再呼び出し（最大 2 ラウンド）
                    │
フェーズ 3: 実行
  最終応答テキストを AgentReport に包んで返す
```

### load_skill 検出ロジック

```csharp
// AgentBase.cs
private static string? ExtractLoadSkillRequest(string text)
{
    var match = Regex.Match(text, @"load_skill\([""']([^""']+)[""']\)");
    return match.Success ? match.Groups[1].Value : null;
}
```

LLM の応答に `load_skill("web-search")` または `load_skill('web-search')` が含まれていた場合、スキルコンテンツをコンテキストに追加して再呼び出しします。

## モデル解決の優先順位

`DbSkillsProvider.ResolveChatClient()` は以下の優先順位で `IChatClient` を解決します:

```
1. スキル固有の ModelKey（SkillDefinition.ModelKey）
   └─ 例: web-search → "haiku", source-verification → "opus"

2. エージェントのデフォルトモデルキー（AgentDefinition.DefaultModelKey）
   └─ 例: ResearchAgent → "sonnet", FactCheckAgent → "opus"

3. appsettings の "default" キーが指す IChatClient
   └─ デフォルト: "sonnet"
```

```csharp
// DbSkillsProvider.cs
public IChatClient ResolveChatClient(string? skillModelKey, ...)
{
    var keyedSp = sp.GetRequiredService<IKeyedServiceProvider>();

    // 1. スキル固有
    if (!string.IsNullOrEmpty(skillModelKey))
    {
        var client = keyedSp.GetKeyedService<IChatClient>(skillModelKey);
        if (client != null) return client;
    }

    // 2. エージェントデフォルト
    var agentModelKey = _cachedAgent?.DefaultModelKey;
    if (!string.IsNullOrEmpty(agentModelKey))
    {
        var client = keyedSp.GetKeyedService<IChatClient>(agentModelKey);
        if (client != null) return client;
    }

    // 3. DI のデフォルト
    return sp.GetRequiredService<IChatClient>();
}
```

## ValidationAgent の判定フォーマット

ValidationAgent は LLM に対して以下の JSON を**のみ**返すよう指示します:

```json
{
  "verdict": "Complete",
  "reason": "ユーザーの要求が十分に充足されています。",
  "retryTarget": null,
  "retryInstruction": null
}
```

| フィールド | 型 | 内容 |
|---|---|---|
| `verdict` | `"Complete"` \| `"Retry"` \| `"Abort"` | 判定結果 |
| `reason` | string | 判定理由 |
| `retryTarget` | string \| null | Retry 時: 再実行対象エージェントID |
| `retryInstruction` | string \| null | Retry 時: スーパーバイザーへの追加指示 |

JSON は `ExtractJson()` で抽出されます（```json ブロックおよび裸の `{}` に対応）。

## エージェント別モデル配分

| エージェント | DefaultModel | スキル | スキルのモデル |
|---|---|---|---|
| ResearchAgent | sonnet | web-search | **haiku**（軽量検索） |
| ResearchAgent | sonnet | text-summarization | sonnet |
| FactCheckAgent | **opus** | source-verification | **opus**（高精度検証） |
| FactCheckAgent | **opus** | claim-analysis | sonnet |
| ReportAgent | sonnet | data-analysis | **opus**（高精度分析） |
| ReportAgent | sonnet | document-generation | sonnet |
| ValidationAgent | **opus** | requirement-check | **opus**（高精度評価） |
| ValidationAgent | **opus** | quality-assessment | sonnet |

重要度・精度が要求される処理には `opus`、速度重視の軽量処理には `haiku`、バランス重視には `sonnet` を使用しています。
