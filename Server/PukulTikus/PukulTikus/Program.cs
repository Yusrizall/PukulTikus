using Microsoft.EntityFrameworkCore;
using PukulTikus.Data;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<GameDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? "Data Source=game.db";
    opt.UseSqlite(cs);
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
