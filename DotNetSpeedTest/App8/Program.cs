using Dapper;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("db");
builder.Services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));
builder.Services.AddDbContext<ItemContext>(opt =>
{
    opt.UseMySql(connectionString, ServerVersion.Parse("8.0"))
       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

var app = builder.Build();
app.Map("/", () => $"aspnet{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
app.MapPost("/find", async (ItemContext db, FindItem input) =>
    await db.Items.Where(r => r.Price >= input.Price).OrderBy(r => r.Price).ToArrayAsync()
);
app.MapPost("/findQuery", async (IDbConnection db, FindItem input) =>
    (await db.QueryAsync<Item>("select id, price from items where price = @price order by price", new { price = input.Price })).ToArray()
);
app.MapPost("/findQueryRaw", (IDbConnection db, FindItem input) =>
{
    if (db.State != ConnectionState.Open)
        db.Open();
    using var command = db.CreateCommand();
    command.CommandText = $"select id, price from items where price = {input.Price} order by price";
    using var reader = command.ExecuteReader();
    var result = new List<Item>();
    while (reader.Read())
    {
        result.Add(new Item
        {
            Id = reader.GetString(0),
            Price = reader.GetInt32(1)
        });
    }
    return result.ToArray();
});

app.Run();

record FindItem(int Price);

[Table("items")]
public class Item
{
    [Column("id")]
    public required string Id { get; set; }

    [Column("price")]
    public int Price { get; set; }
}

public class ItemContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();

    public ItemContext(DbContextOptions<ItemContext> opts) : base(opts) { }
}