using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Gsx.Bridge;

public interface ISimDataProvider
{
    Task<string?> GetCurrentIcaoAsync(CancellationToken ct = default);
    Task<IEnumerable<object>> GetParkingAsync(CancellationToken ct = default);
    Task<bool> ExecuteActionAsync(string action, object? payload, CancellationToken ct = default);
}

public class StubSimDataProvider : ISimDataProvider
{
    public Task<string?> GetCurrentIcaoAsync(CancellationToken ct = default) => Task.FromResult<string?>("EHAM");
    public Task<IEnumerable<object>> GetParkingAsync(CancellationToken ct = default) => Task.FromResult<IEnumerable<object>>(new []
    {
        new { name = "B15", type = 9, hasJetway = true },
        new { name = "B16", type = 8, hasJetway = false }
    }.Cast<object>());
    public Task<bool> ExecuteActionAsync(string action, object? payload, CancellationToken ct = default) => Task.FromResult(true);
}

public class Program
{
    public record ActionRequest(string action, object? payload);

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<ISimDataProvider, StubSimDataProvider>();
        builder.Services.AddCors(o => o.AddPolicy("dev", p => p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

        var app = builder.Build();

        app.UseCors("dev");

        app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

        app.MapGet("/api/icao", async (ISimDataProvider sim, CancellationToken ct) =>
        {
            var icao = await sim.GetCurrentIcaoAsync(ct);
            return Results.Ok(new { icao });
        });

        app.MapGet("/api/parking", async (ISimDataProvider sim, CancellationToken ct) =>
        {
            var stands = await sim.GetParkingAsync(ct);
            return Results.Ok(stands);
        });

        app.MapPost("/api/action", async (HttpContext ctx, ILoggerFactory lf, ISimDataProvider sim, CancellationToken ct) =>
        {
            var logger = lf.CreateLogger("Action");
            var req = await JsonSerializer.DeserializeAsync<ActionRequest>(ctx.Request.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct);
            if (req is null || string.IsNullOrWhiteSpace(req.action))
            {
                return Results.BadRequest(new { error = "action required" });
            }

            var ok = await sim.ExecuteActionAsync(req.action, req.payload, ct);
            logger.LogInformation("Action received: {Action} Payload: {Payload} Result: {Result}", req.action, JsonSerializer.Serialize(req.payload), ok);
            return Results.Ok(new { accepted = ok });
        });

        app.Run("http://localhost:8787");
    }
}
