using KampusRota.Api.Data;
using KampusRota.Api.DTOs;
using KampusRota.Api.Interfaces;
using KampusRota.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=kampusrota.db";
builder.Services.AddDbContext<KampusRotaDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRideService, RideService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiClient", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KampusRotaDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("MauiClient");

app.MapGet("/", () => Results.Ok(new { mesaj = "KampusRota Minimal API calisiyor." }));

app.MapPost("/api/users/register", async (RegisterRequest request, IUserService userService) =>
{
    var result = await userService.RegisterAsync(request);
    return result.Success
        ? Results.Created($"/api/users/{result.Data!.Id}", result.Data)
        : Results.BadRequest(new { mesaj = result.Message });
});

app.MapPost("/api/users/login", async (string email, string sifre, IUserService userService) =>
{
    var result = await userService.LoginAsync(new LoginRequest(email, sifre));
    return result.Success
        ? Results.Ok(result.Data)
        : Results.Unauthorized();
});

app.MapGet("/api/users/{id:int}", async (int id, IUserService userService) =>
{
    var result = await userService.GetByIdAsync(id);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.NotFound(new { mesaj = result.Message });
});

app.MapPut("/api/users/{id:int}/change-password", async (int id, ChangePasswordRequest request, IUserService userService) =>
{
    var result = await userService.ChangePasswordAsync(id, request);
    return result.Success
        ? Results.NoContent()
        : Results.BadRequest(new { mesaj = result.Message });
});

app.MapGet("/api/rides", async (
    string? kalkis,
    string? varis,
    DateTime? tarih,
    bool? sadeceKadinlar,
    IRideService rideService) =>
{
    var rides = await rideService.GetAllAsync(kalkis, varis, tarih, sadeceKadinlar);
    return Results.Ok(rides);
});

app.MapGet("/api/rides/user/{kullaniciId:int}", async (int kullaniciId, IRideService rideService) =>
{
    var rides = await rideService.GetByUserAsync(kullaniciId);
    return Results.Ok(rides);
});

app.MapGet("/api/rides/{id:int}", async (int id, IRideService rideService) =>
{
    var result = await rideService.GetByIdAsync(id);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.NotFound(new { mesaj = result.Message });
});

app.MapPost("/api/rides", async (int kullaniciId, RideCreateRequest request, IRideService rideService) =>
{
    var result = await rideService.CreateAsync(kullaniciId, request);
    return result.Success
        ? Results.Created($"/api/rides/{result.Data!.Id}", result.Data)
        : Results.BadRequest(new { mesaj = result.Message });
});

app.MapPut("/api/rides/{id:int}", async (
    int id,
    int guncelleyenKullaniciId,
    RideUpdateRequest request,
    IRideService rideService) =>
{
    var result = await rideService.UpdateAsync(id, guncelleyenKullaniciId, request);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.BadRequest(new { mesaj = result.Message });
});

app.MapDelete("/api/rides/{id:int}", async (int id, int silenKullaniciId, IRideService rideService) =>
{
    var result = await rideService.DeleteAsync(id, silenKullaniciId);
    return result.Success
        ? Results.NoContent()
        : Results.BadRequest(new { mesaj = result.Message });
});

app.Run();
