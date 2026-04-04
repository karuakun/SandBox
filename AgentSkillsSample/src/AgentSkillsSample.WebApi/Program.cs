using AgentSkillsSample.WebApi.Agents;
using AgentSkillsSample.WebApi.Data;
using AgentSkillsSample.WebApi.Endpoints;
using AgentSkillsSample.WebApi.Skills;
using AgentSkillsSample.WebApi.Sse;
using Anthropic.Bedrock;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ──────────────────────────────────────────────────────────────
// 1. OpenAPI
// ──────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ──────────────────────────────────────────────────────────────
// 2. EF Core / SQLite
// ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AgentDbContext>(o =>
    o.UseSqlite(cfg.GetConnectionString("AgentDb") ?? "Data Source=skills.db"));

// ──────────────────────────────────────────────────────────────
// 3. Skills / Agent リポジトリ
// ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<ISkillsRepository, SkillsRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();

// ──────────────────────────────────────────────────────────────
// 4. SSE イベントストア（Redis:Enabled フラグで切り替え）
// ──────────────────────────────────────────────────────────────
if (cfg.GetValue("Redis:Enabled", false))
{
    var redisConn = cfg["Redis:ConnectionString"] ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddScoped<ISseEventStore, RedisSseEventStore>();
}
else
{
    builder.Services.AddSingleton<ISseEventStore, InMemorySseEventStore>();
}

// ──────────────────────────────────────────────────────────────
// 5. AWS Bedrock 複数モデル（Keyed Services）
//    環境変数: AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY / AWS_DEFAULT_REGION
// ──────────────────────────────────────────────────────────────
var modelsSection = cfg.GetSection("Bedrock:Models").Get<Dictionary<string, string>>()
    ?? new Dictionary<string, string>
    {
        ["default"] = "sonnet",
        ["sonnet"]  = "us.anthropic.claude-sonnet-4-5-20250929-v1:0"
    };

// FromEnv() は ValueTask を返すため await で解決
var bedrockCreds = await AnthropicBedrockCredentialsHelper.FromEnv()
    ?? throw new InvalidOperationException(
        "AWS credentials not found. Set AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY.");

foreach (var (key, modelId) in modelsSection.Where(kv => kv.Key != "default"))
{
    var capturedModelId = modelId;
    var capturedCreds   = bedrockCreds;
    builder.Services.AddKeyedSingleton<IChatClient>(key, (_, _) =>
        new AnthropicBedrockClient(capturedCreds)
            .AsIChatClient(capturedModelId));
}

var defaultModelKey = modelsSection.GetValueOrDefault("default", "sonnet");
builder.Services.AddSingleton<IChatClient>(sp =>
    sp.GetRequiredKeyedService<IChatClient>(defaultModelKey));

// ──────────────────────────────────────────────────────────────
// 6. DbSkillsProvider ファクトリー（agentId 別に生成）
// ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<Func<string, DbSkillsProvider>>(sp => agentId =>
    new DbSkillsProvider(
        sp.GetRequiredService<ISkillsRepository>(),
        sp.GetRequiredService<IAgentRepository>(),
        sp,
        agentId));

// ──────────────────────────────────────────────────────────────
// 7. サブエージェント
// ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<ResearchAgent>(sp =>
    new ResearchAgent(sp.GetRequiredService<Func<string, DbSkillsProvider>>()("research-agent")));

builder.Services.AddScoped<FactCheckAgent>(sp =>
    new FactCheckAgent(sp.GetRequiredService<Func<string, DbSkillsProvider>>()("fact-check-agent")));

builder.Services.AddScoped<ReportAgent>(sp =>
    new ReportAgent(sp.GetRequiredService<Func<string, DbSkillsProvider>>()("report-agent")));

builder.Services.AddScoped<ValidationAgent>(sp =>
    new ValidationAgent(sp.GetRequiredService<Func<string, DbSkillsProvider>>()("validation-agent")));

builder.Services.AddScoped<SupervisorAgent>();

// ──────────────────────────────────────────────────────────────
// 8. Microsoft.AgentFramework（IAgent の登録）
// ──────────────────────────────────────────────────────────────
builder.AddAgent<SupervisorAgent>();

// ──────────────────────────────────────────────────────────────
// アプリケーション構築
// ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// ──────────────────────────────────────────────────────────────
// DB マイグレーション + シードデータ（起動時に自動実行）
// ──────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(db);
}

// ──────────────────────────────────────────────────────────────
// エンドポイント
// ──────────────────────────────────────────────────────────────
app.MapChatEndpoints();

// Microsoft.AgentFramework: Bot Protocol エンドポイント（/api/messages）
app.MapAgentEndpoints();

app.Run();
