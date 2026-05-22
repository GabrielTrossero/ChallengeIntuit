using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    EnsureTurnosNoShowColumns(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.Run();

static void EnsureTurnosNoShowColumns(AppDbContext db)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose)
        connection.Open();

    try
    {
        var columns = GetTableColumns(connection, "Turnos");

        if (!columns.Contains("PenalizaNoShow", StringComparer.OrdinalIgnoreCase))
            db.Database.ExecuteSqlRaw("ALTER TABLE Turnos ADD COLUMN PenalizaNoShow INTEGER NOT NULL DEFAULT 0;");

        if (!columns.Contains("TipoPenalizacionNoShow", StringComparer.OrdinalIgnoreCase))
            db.Database.ExecuteSqlRaw("ALTER TABLE Turnos ADD COLUMN TipoPenalizacionNoShow TEXT NULL;");
    }
    finally
    {
        if (shouldClose)
            connection.Close();
    }
}

static HashSet<string> GetTableColumns(DbConnection connection, string tableName)
{
    using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA table_info('{tableName}');";

    using var reader = command.ExecuteReader();
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    while (reader.Read())
    {
        if (!reader.IsDBNull(1))
            columns.Add(reader.GetString(1));
    }

    return columns;
}
