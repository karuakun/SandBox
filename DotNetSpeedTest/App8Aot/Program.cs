using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;

var builder = WebApplication.CreateSlimBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("db");
builder.Services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
var app = builder.Build();
app.Map("/", () => $"aspnet{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
app.MapPost("/findQuery", (IDbConnection db, FindItem input) =>
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
}
);
app.Run();

[JsonSerializable(typeof(FindItem))]
[JsonSerializable(typeof(Item[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

record FindItem(int Price);

public class Item
{
    public required string Id { get; set; }
    public int Price { get; set; }
}
