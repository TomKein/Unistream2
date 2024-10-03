using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("InMemoryDb"));

var app = builder.Build();

app.MapGet("/", async (HttpContext context, AppDbContext db) =>
{
    var query = context.Request.Query;

    if (query.ContainsKey("insert"))
    {
        string json = query["insert"];

        try
        {
            var settings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                Formatting = Formatting.Indented
            };
            var entity = JsonConvert.DeserializeObject<Entity>(json, settings);

            db.Entities.Add(entity);
            await db.SaveChangesAsync();

            return Results.Ok("Сущность сохранена.");
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Ошибка при сохранении сущности: {ex.Message}");
        }
    }
    else if (query.ContainsKey("get"))
    {
        string idStr = query["get"];

        if (Guid.TryParse(idStr, out Guid id))
        {
            var entity = await db.Entities.FindAsync(id);

            if (entity != null)
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(entity, settings);
                return Results.Content(json, "application/json");
            }
            else
            {
                return Results.NotFound("Сущность не найдена.");
            }
        }
        else
        {
            return Results.BadRequest("Некорректный Id.");
        }
    }
    else
    {
        return Results.BadRequest("Некорректный запрос.");
    }
});

app.Run("http://127.0.0.1:5000");

public class Entity
{
    public Guid Id { get; set; }
    public DateTime OperationDate { get; set; }
    public decimal Amount { get; set; }

    private Entity()
    {
        this.Id = Guid.NewGuid();
        this.OperationDate = DateTime.Now;
    }
    
    public Entity(Guid id, DateTime operationDate, decimal amount)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        OperationDate = operationDate == default ? DateTime.Now : operationDate;
        Amount = amount;
    }
}

public class AppDbContext : DbContext
{
    public DbSet<Entity> Entities { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}